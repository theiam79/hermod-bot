# E2E Testing Plan — Hermod Bot

> Generated 2026-03-02. Updated 2026-03-02 (WireMock analysis). Covers the `vAspire-claude` branch.

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current Test Landscape](#2-current-test-landscape)
3. [Recommended Tooling](#3-recommended-tooling)
4. [Test Architecture](#4-test-architecture)
5. [Discord Simulation with WireMock](#5-discord-simulation-with-wiremock)
6. [Flows to Test](#6-flows-to-test)
7. [Gaps & Non-E2E-Testable Areas](#7-gaps--non-e2e-testable-areas)
8. [Project Structure](#8-project-structure)
9. [Implementation Sequence](#9-implementation-sequence)

---

## 1. Executive Summary

The project has **111 integration/unit tests** that exercise the API in-process via
`WebApplicationFactory<Program>`. These tests are fast and give good coverage of
individual handlers and endpoints, but they have a blind spot: **nothing tests the
cross-process NATS transport** between API and Bot, and **nothing tests that the
Aspire AppHost wires resources correctly**.

This plan adds a new `Hermod.E2E.Tests` project that uses **Aspire.Hosting.Testing**
(`DistributedApplicationTestingBuilder`) to boot the real AppHost, including
PostgreSQL, NATS, and the API as a child process. The Bot is handled specially
(see §4.3). Tests make real HTTP calls against the running API and verify outcomes
through the API's own endpoints and direct database queries.

### Key decisions made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Test runner | **TUnit** | Consistent with existing tests; async-first; `[ClassDataSource]` lifecycle fits Aspire fixture |
| Infrastructure | **Aspire.Hosting.Testing** | Reuses the real AppHost; auto-provisions PostgreSQL + NATS; tests wiring fidelity |
| Bot in E2E | **WireMock-backed Bot** (see §4.3, §5) | Real Bot needs Discord gateway; WireMock simulates Discord REST API so handlers execute real HTTP calls against a fake Discord |
| Discord REST | **WireMock.Net** via Aspire resource (see §5) | Simulates Discord REST API (`/channels/*/messages`, etc.); NetCord's `RestClient` redirected via `Hostname` override |
| Auth in E2E | **Test cookie jar + seeded AuthUser** | Discord OAuth can't be automated; seed auth-db directly and attach a session cookie |
| Browser tests | **Out of scope** (see §6) | SvelteKit SPA is adapter-static; API contract tests cover the backend; Playwright can be added later for the frontend |
| Existing tests | **Keep as-is** | WebApplicationFactory tests are fast and give DI-level access; E2E tests complement but don't replace them |

---

## 2. Current Test Landscape

### What exists

| Project | Framework | Tests | Scope |
|---------|-----------|-------|-------|
| `Hermod.Api.Tests` | TUnit + WebApplicationFactory + Testcontainers | 97 | API endpoints, Wolverine handlers (in-process), auth services |
| `Hermod.BGStats.Tests` | TUnit | 14 | BGStats parser (pure unit tests) |

### What's missing

- **Cross-process NATS transport**: API→Bot (`SharePlayToGroup`, `DistributePlayFile`) and Bot→API (`RegisterCommunity`, `EnrollInGroup`, `ClaimPlayer`, `UpdateGroupSharing`) message flows are never exercised end-to-end.
- **AppHost wiring fidelity**: `WaitFor`, `WithReference`, connection string injection, and environment variable propagation are untested.
- **Database migration correctness**: Tests use Testcontainers with `EnsureCreatedAsync` / `Migrate()` per fixture; no test validates that the real AppHost migration path works.
- **Full pipeline integration**: The upload→extract→persist→share cascade runs in-process but never crosses a process boundary or exercises NATS serialization.

---

## 3. Recommended Tooling

### Core

| Package | Version | Purpose |
|---------|---------|---------|
| **Aspire.Hosting.Testing** | `13.1.*` | `DistributedApplicationTestingBuilder` — boots the real AppHost in a test process |
| **WireMock.Net.Aspire** | `1.8.*` | Adds WireMock as an Aspire-managed container resource; provides Discord REST API simulation |
| **TUnit** | `1.*` | Test framework (matches existing projects) |
| **NATS.Net** | `2.*` | Direct NATS publish for bot→API flow tests |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | `10.*` | Create scoped DbContexts from connection strings for seeding/asserting |

### Why Aspire.Hosting.Testing over alternatives

| Alternative | Rejected because |
|-------------|-----------------|
| Extending `WebApplicationFactory` with a second process | Manual process management; doesn't test AppHost wiring; brittle port coordination |
| Docker Compose + Testcontainers | Doesn't test the Aspire AppHost at all; duplicates orchestration logic; harder to maintain |
| Playwright-only (browser) | Tests the frontend, not the backend pipeline; orthogonal concern |
| Custom `IHost` bootstrap per service | Reimplements what the AppHost already does; wiring drift risk |

### Why WireMock.Net over alternatives

| Alternative | Rejected because |
|-------------|-----------------|
| **No-op interface stubs** | Handlers run but skip all Discord HTTP calls; doesn't verify request serialization, URL construction, or error handling paths |
| **RichardSzalay.MockHttp** | Mocks at `HttpMessageHandler` level; NetCord's `RestClient` manages its own `HttpClient` internally, so we can't inject a handler without forking NetCord |
| **Mountebank** | External Node.js process; unnecessary complexity for a .NET-only stack |
| **Custom `IRestRequestHandler`** | Would work but requires injecting NetCord internals; `Hostname` override is simpler and less coupled |

### Optional / Future

| Package | Purpose | When to add |
|---------|---------|-------------|
| **Playwright for .NET** | Browser-based SvelteKit frontend tests | When the web frontend has enough pages to warrant it |
| **Verify** | Snapshot testing for API responses | If response shape stability becomes a concern |
| **NBomber** | Load/performance testing | If performance SLAs are defined |

---

## 4. Test Architecture

### 4.1 Aspire Fixture

A single `AspireFixture` shared per test session boots the AppHost once:

```
┌─────────────────────────────────────────────────┐
│  Test Process (TUnit)                           │
│                                                 │
│  AspireFixture                                  │
│  ├── DistributedApplicationTestingBuilder       │
│  │   └── AppHost.cs (real orchestration code)   │
│  │                                              │
│  │  Child processes / containers:               │
│  │  ├── postgres (3 databases)                  │
│  │  ├── nats                                    │
│  │  ├── hermod-api  (real API process)          │
│  │  └── hermod-bot-stub  (see §4.3)            │
│  │                                              │
│  ├── CreateApiClient()     → HttpClient         │
│  ├── CreateAuthenticatedClient(userId)          │
│  ├── GetHermodDbConnection()  → connection str  │
│  ├── GetAuthDbConnection()    → connection str  │
│  └── GetBotDbConnection()     → connection str  │
└─────────────────────────────────────────────────┘
```

**Lifecycle:**

1. `InitializeAsync()`:
   - `DistributedApplicationTestingBuilder.CreateAsync<Projects.Hermod_AppHost>()` with test config overrides
   - `BuildAsync()` → `StartAsync()`
   - `WaitForResourceHealthyAsync("hermod-api")` with 60s timeout
   - Extract connection strings via `GetConnectionStringAsync()`
   - Run seed data (create test AuthUser records in auth-db)

2. `DisposeAsync()`:
   - `app.DisposeAsync()` tears down all child processes and containers

### 4.2 Authentication Strategy

Discord OAuth cannot be automated in E2E tests. Instead:

1. **Seed `auth-db` directly**: Insert an `AuthUser` + `ExternalLogin` record using a raw `AuthDbContext` constructed from the test connection string.
2. **Obtain a session cookie**: Make a request to a **test-only auth endpoint** (only registered when `ASPNETCORE_ENVIRONMENT=Testing`) that issues a cookie for a given UserId.
   - Alternative: If adding a test endpoint is undesirable, configure the API to accept `TestAuthHandler` in the Testing environment (already exists in `Hermod.Api.Tests`). The E2E fixture sets `ASPNETCORE_ENVIRONMENT=Testing` via AppHost config, and the API's `Program.cs` conditionally registers the test auth scheme.
   - **Recommended approach**: Add an `if (app.Environment.IsEnvironment("Testing"))` block in the API's `Program.cs` that registers a `/auth/test-login` endpoint. This endpoint accepts a UserId, creates a cookie, and returns it. The E2E test client calls this endpoint first, then uses the cookie jar for subsequent requests. This avoids shipping test auth code in production builds and keeps the mechanism explicit.

3. **Pass the cookie**: `HttpClient` with a `CookieContainer` in `HttpClientHandler` preserves the session across requests.

### 4.3 Bot Strategy — WireMock-Backed Bot

The real `Hermod.Bot` has two Discord integration points:

1. **Gateway WebSocket** — connects to `wss://gateway.discord.gg` for real-time events (guild join/leave, ready). This **cannot** be simulated by WireMock (HTTP-only).
2. **REST API calls** — `RestClient` calls `https://discord.com/api/v10/channels/*/messages`, etc. This **can** be simulated by WireMock.

**Solution: Skip gateway, redirect REST to WireMock.**

The Bot's `Program.cs` gets a `Testing` environment check that:

1. **Skips NetCord gateway + slash command registration** (no WebSocket connection, no Discord interaction handlers)
2. **Registers `RestClient` with `Hostname` pointed at WireMock** (so `rest.SendMessageAsync()` hits the WireMock server instead of Discord)
3. **Keeps Wolverine + NATS handlers** (so `SharePlayToGroupHandler`, `DistributePlayFileHandler`, etc. still process messages and make real HTTP calls — just to WireMock instead of Discord)

This means the E2E test exercises the real code path:
- API receives upload → processes pipeline → publishes `SharePlayToGroup` to NATS
- Bot receives `SharePlayToGroup` via NATS → `SharePlayToGroupHandler` runs → calls `rest.SendMessageAsync()` → **real HTTP request to WireMock** → handler saves `PlayPostEntity` with the message ID from WireMock's response
- Verification: query `bot-db` for `PlayPostEntity` records AND inspect WireMock's request log to verify the exact embed payload sent

**Why not `SkipBot=true`?** That skips the bot entirely, losing NATS transport coverage and handler execution.

**Why not no-op stubs?** Stubs skip the HTTP call entirely. WireMock lets the handler execute its real `rest.SendMessageAsync()` code, exercising NetCord's request serialization, URL construction, header injection, and response deserialization. It also catches regressions where a handler sends the wrong payload shape.

**Gateway cache workaround**: `SharePlayToGroupHandler` accesses `gateway.Cache.Guilds` to validate guild/channel existence before posting. Without a gateway connection, the cache is empty. Two options:

- **Option A (recommended)**: In `Testing` mode, skip the cache validation. The handler already has a graceful fallback (logs warning, returns). Wrap the cache check in an `if (gateway.Cache.Guilds.Count > 0 || !isTesting)` guard. This is a 2-line change.
- **Option B**: Extract `IGuildCacheAccessor` interface with `TryGetGuild`/`TryGetChannel` methods. In testing, inject an in-memory stub pre-populated with test guild/channel data. Cleaner but more refactoring.

**Decision**: Start with **Option A** — minimal code change, unblocks E2E immediately. Refactor to Option B later if the testing mode conditional becomes a pattern across multiple handlers.

### 4.4 Data Isolation Between Tests

Each test class operates in its own logical "tenant" using a unique UserId and unique group names. Database state is not reset between tests — instead, tests use unique identifiers to avoid collisions.

For tests that require a pristine state (e.g., pagination counts), use a dedicated test user created in that test's setup.

---

## 5. Discord Simulation with WireMock

### 5.1 Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Test Process (TUnit)                                            │
│                                                                  │
│  AspireFixture                                                   │
│  ├── DistributedApplicationTestingBuilder                        │
│  │   └── AppHost.cs (real orchestration code)                    │
│  │                                                               │
│  │  Aspire-managed resources:                                    │
│  │  ├── postgres (hermod-db, auth-db, bot-db)                    │
│  │  ├── nats                                                     │
│  │  ├── discord-api-mock (WireMock container)  ◄── NEW           │
│  │  ├── hermod-api  (real API child process)                     │
│  │  └── hermod-bot  (real Bot child process, REST → WireMock)    │
│  │                                                               │
│  ├── WireMock admin API  → configure stubs, inspect requests     │
│  ├── CreateApiClient()   → HttpClient to API                     │
│  └── Database helpers    → seed/query all 3 databases            │
└──────────────────────────────────────────────────────────────────┘
```

### 5.2 NetCord REST Client Redirection

NetCord's `RestClient` constructs its base URL as:
```
https://{configuration.Hostname}/api/v{version}
```

`Hostname` defaults to `discord.com` but is configurable via `RestClientConfiguration.Hostname` (exposed as `GatewayClientOptions.RestClientConfiguration.Hostname` in the hosting layer).

**Approach**: In Testing mode, the Bot's `Program.cs` overrides the hostname to point at the WireMock container. Since Aspire injects the WireMock URL via service discovery or an environment variable, the bot reads it at startup:

```csharp
// In Hermod.Bot/Program.cs (Testing mode)
var discordApiUrl = builder.Configuration["Discord:ApiBaseUrl"]; // e.g., "localhost:12345"

// Register a standalone RestClient pointed at WireMock
// (gateway is not registered in Testing mode)
builder.Services.AddSingleton(sp =>
{
    var config = new RestClientConfiguration
    {
        Token = "e2e-test-token",
        Hostname = discordApiUrl,  // WireMock
    };
    return new RestClient(config);
});
```

**TLS note**: NetCord hardcodes `https://` in the URL. WireMock can be configured with `UseSSL = true`, or we use a custom `IRestRequestHandler` to downgrade to HTTP. The simpler path: configure WireMock.Net.Aspire with HTTPS (it supports self-signed certs), and the bot's `HttpClient` trusts all certs in Testing mode.

### 5.3 Discord REST API Stubs

The bot makes exactly **4 types of Discord REST API calls**. WireMock stubs for each:

#### Send message to channel (`SharePlayToGroupHandler`)
```
POST /api/v10/channels/{channelId}/messages
→ 200 with { "id": "<generated>", "channel_id": "<from-path>" }
```

#### Edit message (`SharePlayToGroupHandler` — update existing play post)
```
PATCH /api/v10/channels/{channelId}/messages/{messageId}
→ 200 with { "id": "<messageId>", "channel_id": "<channelId>" }
```

#### Get DM channel (`DistributePlayFileHandler`)
```
POST /api/v10/users/@me/channels
→ 200 with { "id": "<generated-dm-channel-id>", "type": 1 }
```

#### Send DM (`DistributePlayFileHandler`)
```
POST /api/v10/channels/{dmChannelId}/messages (with file attachment)
→ 200 with { "id": "<generated>" }
```

WireMock response templating generates unique message IDs using `{{Random.Guid}}` or a counter, so the `PlayPostEntity.DiscordMessageId` stored in bot-db has a real (fake) value to assert against.

### 5.4 WireMock Aspire Resource in AppHost

The WireMock resource is added **conditionally** in the AppHost (only during testing):

```csharp
// In AppHost.cs (conceptual — actual implementation may use OnBuilderCreated hook)
IResourceBuilder<WireMockServerResource>? discordMock = null;
if (builder.ExecutionContext.IsRunMode && builder.Configuration["Testing"] == "true")
{
    discordMock = builder.AddWireMock("discord-api-mock")
        .WithApiMappingBuilder(async api =>
        {
            // POST /api/v10/channels/*/messages → 200 with message object
            api.Given(b => b
                .WithRequest(r => r.UsingPost().WithPath("/api/v10/channels/*/messages"))
                .WithResponse(r => r
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"id":"111111111111111111","channel_id":"mock"}""")));

            // PATCH /api/v10/channels/*/messages/* → 200
            api.Given(b => b
                .WithRequest(r => r.UsingPatch().WithPath("/api/v10/channels/*/messages/*"))
                .WithResponse(r => r
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"id":"111111111111111111","channel_id":"mock"}""")));

            // POST /api/v10/users/@me/channels → 200 with DM channel
            api.Given(b => b
                .WithRequest(r => r.UsingPost().WithPath("/api/v10/users/@me/channels"))
                .WithResponse(r => r
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"id":"999999999999999999","type":1}""")));
        });
}

var bot = builder.AddProject<Projects.Hermod_Bot>("hermod-bot")
    .WithReference(botDb)
    .WithReference(nats)
    .WithEnvironment("Discord__Token", discordToken)
    .WaitFor(botDb)
    .WaitFor(nats);

if (discordMock is not null)
{
    bot.WithReference(discordMock)
       .WithEnvironment("Discord__ApiBaseUrl", discordMock.GetEndpoint("http"))
       .WaitFor(discordMock);
}
```

**Alternative (cleaner)**: Use `DistributedApplicationTestingBuilder`'s `OnBuilderCreated` hook in the test project to add the WireMock resource, avoiding any testing-specific code in the production AppHost. This is the recommended approach.

### 5.5 What WireMock Verification Enables

Beyond just "not crashing", WireMock lets tests assert on the **exact Discord API calls** made:

| Assertion | How |
|-----------|-----|
| Bot sent an embed to the correct channel | Inspect WireMock request log: `POST /api/v10/channels/{expectedChannelId}/messages` |
| Embed contains correct game name, players, scores | Deserialize the request body from WireMock log; assert on `embeds[0].title`, `embeds[0].fields` |
| Bot updated (not re-created) an existing message | `PATCH` call logged (not a second `POST`) |
| Bot fell back to new message on 404 | First call: `PATCH → 404` (configured via WireMock scenario), second call: `POST → 200` |
| DM sent with correct file attachment | `POST /api/v10/channels/*/messages` with multipart body containing `.bgsplay` content |
| No Discord calls when group has `AllowSharing=false` | Zero requests logged in WireMock |

### 5.6 Error Simulation

WireMock can simulate Discord API error conditions to test handler resilience:

| Scenario | WireMock stub |
|----------|---------------|
| Rate-limited (429) | `WithStatusCode(429)` + `Retry-After` header |
| Message deleted (404 on edit) | Stateful scenario: first PATCH → 404, handler falls back to POST |
| DMs disabled (403) | `POST /users/@me/channels → 403` |
| Discord outage (500) | `WithStatusCode(500)` |
| Timeout | `WithDelay(TimeSpan.FromSeconds(30))` |

These make the E2E tests catch real failure modes that no-op stubs would silently skip.

### 5.7 What WireMock Cannot Simulate

| Area | Why | Mitigation |
|------|-----|------------|
| **Gateway WebSocket events** | WireMock is HTTP-only; gateway uses `wss://` | Gateway handlers tested in isolation or via manual QA |
| **Slash command registration** | Requires `PUT /applications/{id}/commands` during startup with a valid bot token | Skip in Testing mode; covered by Discord.Net SDK |
| **Gateway cache population** | Cache filled by WebSocket events, not REST | Option A bypass (see §4.3) |
| **OAuth callback** | Browser-initiated flow | Test auth seeding (see §4.2) |

---

## 6. Flows to Test

### 6.1 Critical Path — Upload-to-Share Pipeline

**Priority: P0 (must-have)**

This is the core value proposition of the platform. The full cascade:

```
POST /api/plays/upload (.bgsplay file)
  → PlayFileUploaded
    → ExtractPlaysHandler → PlayExtracted (per play)
      → PersistPlayHandler → PlayPersisted
        → SharePlayHandler → SharePlayToGroup (per group, via NATS)
          → [Bot stub] SharePlayToGroupHandler → PlayPost record in bot-db
```

**Tests:**

| # | Test | Verify |
|---|------|--------|
| 1 | Upload single-play file, user enrolled in 1 group | PlayEntity in hermod-db; PlayPostEntity in bot-db; play visible via `GET /api/plays` |
| 2 | Upload multi-play file (3 plays), user enrolled in 2 groups | 3 PlayEntities; 6 PlayPostEntities (3 plays × 2 groups) |
| 3 | Upload duplicate play (same BgStatsPlayUuid) | PlayEntity updated (not duplicated); PlayPostEntity updated (change type = Updated) |
| 4 | Upload play, user not enrolled in any group | PlayEntity persisted; no PlayPostEntity (no share target) |
| 5 | Upload play, group has `AllowSharing=false` | PlayEntity persisted; no PlayPostEntity for that group |

**Async verification challenge**: The pipeline is asynchronous (Wolverine cascades + NATS transport). Tests must poll/wait for the final state. Strategy:
- After upload returns `201 Created`, poll `bot-db` for `PlayPostEntity` with a timeout (e.g., 15s with 500ms intervals).
- Use a helper: `await WaitForConditionAsync(() => botDb.PlayPosts.AnyAsync(p => p.PlayId == playId), timeout: 15s)`

### 6.2 Group Management

**Priority: P0**

| # | Test | Verify |
|---|------|--------|
| 6 | Create group via `POST /api/groups` | GroupEntity in hermod-db; 201 response with GroupId |
| 7 | Get group by ID (public, no auth) | 200 with group details |
| 8 | List user's groups (auth required) | Only groups the user is enrolled in |
| 9 | Join group via `PUT /api/groups/{id}/membership` | UserGroupEntity created; group appears in user's list |
| 10 | Join group idempotently (second PUT) | No error; no duplicate membership |
| 11 | Unauthenticated group list → 401 | Correct auth enforcement |

### 6.3 Community Registration (Bot→API via NATS)

**Priority: P1**

This tests the reverse NATS direction (bot-stub → API).

| # | Test | Verify |
|---|------|--------|
| 12 | Bot registers a Discord guild | GroupEntity created with deterministic UUID v5 GroupId; `CommunityRegistered` response |
| 13 | Bot re-registers same guild | GroupEntity name updated (upsert); same GroupId |
| 14 | Bot disables sharing (`UpdateGroupSharing(groupId, false)`) | GroupEntity.AllowSharing = false |

**Approach**: These tests invoke Wolverine handlers on the bot-stub side via NATS. To trigger them, either:
- (a) Publish a message directly to the `hermod.api` NATS subject from the test process (requires a NATS client in the test).
- (b) Expose a test-only HTTP endpoint on the bot-stub that triggers the handler. **Decision: Use option (a)** — it's closer to reality and avoids test-only endpoints on the bot. The test creates a NATS connection using the same connection string and publishes a `RegisterCommunity` message.

### 6.4 Enrollment Flow

**Priority: P1**

| # | Test | Verify |
|---|------|--------|
| 15 | Enroll user in group via `PUT /api/groups/{id}/membership` | UserGroupEntity + lazy-provisioned UserProfileEntity |
| 16 | Enroll user in group that doesn't exist → appropriate error | 404 or similar |

### 6.5 Player Claiming

**Priority: P2**

| # | Test | Verify |
|---|------|--------|
| 17 | Upload play → claim a player on it | PlayerMappingEntity created; PlayPlayerEntity.MappedUserId set |
| 18 | Claim player backfills across existing plays | All PlayPlayerEntities with that BgStatsPlayerUuid get MappedUserId |

**Approach**: Player claiming is initiated from the bot (Discord component interaction). In E2E, trigger via NATS message to `hermod.api` with `ClaimPlayer` command, verify hermod-db state.

### 6.6 Auth Endpoints

**Priority: P1**

| # | Test | Verify |
|---|------|--------|
| 19 | `GET /auth/me` with valid session | 200 with user details |
| 20 | `GET /auth/me` without session | 401 |
| 21 | `POST /auth/logout` | Session cookie cleared; subsequent `/auth/me` returns 401 |

### 6.7 Upload Validation

**Priority: P1**

| # | Test | Verify |
|---|------|--------|
| 22 | Upload file > 1 MB | 400 Bad Request |
| 23 | Upload invalid JSON file | 400 or appropriate error |
| 24 | Upload without auth | 401 |

### 6.8 Play Listing & Pagination

**Priority: P2**

| # | Test | Verify |
|---|------|--------|
| 25 | Upload 5 plays, list with `pageSize=2` | First page has 2 plays, correct totalCount=5 |
| 26 | Page 2 of 5 plays | Correct offset, no duplicates |
| 27 | List plays for user with no uploads | Empty list, totalCount=0 |

### 6.9 AppHost Wiring Smoke Tests

**Priority: P0**

| # | Test | Verify |
|---|------|--------|
| 28 | API health endpoint responds | `GET /health` returns 200 (or `GET /alive`) |
| 29 | API is reachable via `CreateHttpClient("hermod-api")` | Basic connectivity |
| 30 | All resources reach Running/Healthy state | `WaitForResourceHealthyAsync` succeeds for all resources |

---

### 6.10 Discord API Interaction Tests (NEW — enabled by WireMock)

**Priority: P1**

These tests verify the bot's Discord REST API calls hit the right endpoints with the right payloads. Previously untestable without WireMock.

| # | Test | Verify |
|---|------|--------|
| 31 | Upload play → share to group → WireMock receives embed | `POST /api/v10/channels/{channelId}/messages` with embed containing game name, player names, scores |
| 32 | Upload duplicate play → WireMock receives PATCH (edit) | `PATCH /api/v10/channels/{channelId}/messages/{messageId}` with updated embed |
| 33 | Edit fails with 404 → falls back to new POST | WireMock scenario: PATCH→404, then POST→200; PlayPostEntity updated with new message ID |
| 34 | DM file distribution → WireMock receives file | `POST /api/v10/channels/{dmChannelId}/messages` with multipart attachment |
| 35 | Discord 403 on DM → handler logs warning, no crash | WireMock returns 403; no PlayPostEntity impact; handler completes gracefully |

---

## 7. Gaps & Non-E2E-Testable Areas

### 7.1 Discord Gateway Events (Not E2E Testable)

| Area | Why |
|------|-----|
| Guild join/leave events | Requires a real Discord WebSocket gateway; WireMock is HTTP-only |
| Slash command registration | `PUT /applications/{id}/commands` during startup; requires valid bot token |
| Component interactions (claim player dropdown) | Triggered by Discord UI interaction events via WebSocket |
| Gateway cache population | Cache filled by WebSocket events, not REST |

**Now testable via WireMock** (moved from gap to covered):
- ~~Embed posting to channels~~ → covered by §6.10 tests 31-33
- ~~DM file distribution~~ → covered by §6.10 tests 34-35

**Mitigation for remaining gaps**: Gateway event handlers (`GuildCreateHandler`, `GuildDeleteHandler`, `ReadyHandler`) are tested via existing `WebApplicationFactory` integration tests at the handler level. The E2E tests verify the NATS transport and handler execution; WireMock verifies the Discord REST calls. The only untested layers are the WebSocket connection itself and Discord's slash command registration — both are NetCord SDK concerns with minimal custom logic.

### 7.2 Discord OAuth Flow (Not E2E Testable)

| Area | Why |
|------|-----|
| `/auth/login` → Discord redirect → callback | Requires a browser, Discord credentials, and a real OAuth flow |
| `OnCreatingTicket` callback (user provisioning) | Runs inside the OAuth middleware; triggered by Discord's callback |

**Mitigation**: `ExternalLoginService` has 10+ unit tests covering provisioning logic. The E2E auth strategy (§4.2) bypasses OAuth but still tests that cookie-based sessions work correctly for all protected endpoints.

### 7.3 YARP Gateway Routing (Partially Testable)

| Area | Testable? | Notes |
|------|-----------|-------|
| API route forwarding (`/api/*`, `/auth/*`) | Only in publish mode | In run mode, YARP routes everything to Vite; API routing is handled by Vite proxy |
| Static file serving | Only in publish mode | Requires `PublishWithStaticFiles` build output |
| DevTunnel integration | No | Requires Azure DevTunnel service |

**Mitigation**: YARP routing in publish mode could be tested with a dedicated publish-mode AppHost configuration, but the complexity isn't worth it for the current stage. Direct API tests via `CreateHttpClient("hermod-api")` are sufficient.

### 7.4 Web Frontend (Out of Scope for This Plan)

| Area | Why deferred |
|------|-------------|
| SvelteKit page rendering | adapter-static SPA; API contract is the critical boundary |
| Client-side auth state | Covered by `/auth/me` API tests |
| File upload UI | Covered by `POST /api/plays/upload` API tests |

**Future**: When the frontend matures, add Playwright tests in a separate `Hermod.Web.Tests` project. These would run against the full AppHost (including YARP gateway) and verify user flows through the browser.

### 7.5 Concurrent Upload Race Conditions

The Wolverine durable queue race condition (handler fires before transaction commits) is mitigated by retry policies but difficult to reliably trigger in E2E tests. The existing unit tests validate the retry behavior directly.

### 7.6 NATS At-Most-Once Delivery

Core NATS provides no delivery guarantees. If the bot-stub is slow to start and a message is published before it subscribes, the message is lost. This is by design (documented in CLAUDE.md) but means E2E tests must ensure the bot-stub is fully started before triggering message flows.

**Mitigation**: The `WaitForResourceHealthyAsync` call in the fixture ensures all services are running before tests execute. Add a NATS subscription readiness check if flaky test failures appear.

---

## 8. Project Structure

```
tests/
├── Hermod.Api.Tests/           # Existing — keep as-is
├── Hermod.BGStats.Tests/       # Existing — keep as-is
└── Hermod.E2E.Tests/           # NEW
    ├── Hermod.E2E.Tests.csproj
    ├── Infrastructure/
    │   ├── AspireFixture.cs         # DistributedApplicationTestingBuilder lifecycle + WireMock
    │   ├── TestAuthHelper.cs        # Seed auth-db + obtain session cookie
    │   ├── DbHelper.cs              # Seed/query hermod-db, auth-db, bot-db
    │   ├── WireMockHelper.cs        # Configure Discord API stubs, inspect request logs
    │   └── AsyncWaiter.cs           # Poll-until-condition helper for async pipelines
    ├── Pipeline/
    │   ├── UploadToShareTests.cs    # §6.1 — full upload pipeline
    │   ├── DuplicateUploadTests.cs  # §6.1 — upsert behavior
    │   └── PaginationTests.cs       # §6.8 — play listing
    ├── Groups/
    │   ├── GroupManagementTests.cs   # §6.2 — CRUD + membership
    │   └── CommunityRegistrationTests.cs  # §6.3 — bot→API via NATS
    ├── Auth/
    │   └── AuthEndpointTests.cs     # §6.6 — session management
    ├── Enrollment/
    │   └── EnrollmentTests.cs       # §6.4 — group enrollment
    ├── Players/
    │   └── ClaimPlayerTests.cs      # §6.5 — player claiming
    ├── Discord/
    │   ├── EmbedPostingTests.cs     # §6.10 — WireMock-verified Discord calls
    │   └── ErrorHandlingTests.cs    # §6.10 — 404 fallback, 403 DM failures
    └── Smoke/
        └── AppHostSmokeTests.cs     # §6.9 — wiring + health checks
```

### Project file (`Hermod.E2E.Tests.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing" Version="13.1.*" />
    <PackageReference Include="WireMock.Net" Version="1.*" />
    <PackageReference Include="TUnit" Version="1.*" />
    <PackageReference Include="NATS.Net" Version="2.*" />
    <!-- EF Core for seeding/querying databases directly -->
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference AppHost for DistributedApplicationTestingBuilder<T> -->
    <ProjectReference Include="..\..\src\Hermod.AppHost\Hermod.AppHost.csproj" />
    <!-- Reference data projects for DbContext types used in seeding/assertions -->
    <ProjectReference Include="..\..\src\Hermod.Data\Hermod.Data.csproj" />
    <ProjectReference Include="..\..\src\Hermod.Auth\Hermod.Auth.csproj" />
    <ProjectReference Include="..\..\src\Hermod.Messages\Hermod.Messages.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\sample-play-files\**\*"
          CopyToOutputDirectory="PreserveNewest"
          LinkBase="TestData" />
  </ItemGroup>
</Project>
```

> **Note on WireMock.Net vs WireMock.Net.Aspire**: The plan uses `WireMock.Net` (in-process server) rather than `WireMock.Net.Aspire` (container resource). The in-process approach is simpler — the test fixture starts a `WireMockServer`, gets the URL, and passes it to the AppHost via configuration. The Aspire container approach adds a Docker image pull and slower startup for no added benefit since the WireMock server only needs to be reachable by the bot child process (which runs on the same host). If network isolation becomes an issue, switch to the container approach.

### AspireFixture sketch

```csharp
public class AspireFixture : IAsyncInitializer, IAsyncDisposable
{
    public DistributedApplication App { get; private set; } = null!;
    public WireMockServer DiscordApi { get; private set; } = null!;

    private string _hermodDbConn = null!;
    private string _authDbConn = null!;
    private string _botDbConn = null!;

    public async Task InitializeAsync()
    {
        // 1. Start WireMock BEFORE the AppHost so the URL is available
        DiscordApi = WireMockServer.Start();
        WireMockHelper.ConfigureDiscordStubs(DiscordApi);

        // 2. Boot the real AppHost
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hermod_AppHost>(
                [],
                (options, settings) =>
                {
                    // Required parameters (dummy values — bot doesn't connect to Discord)
                    settings.Configuration["Parameters:discord-token"] = "e2e-test-token";
                    settings.Configuration["Parameters:discord-client-id"] = "e2e-test-client-id";
                    settings.Configuration["Parameters:discord-client-secret"] = "e2e-test-secret";

                    // Bot: skip gateway, point REST at WireMock
                    settings.Configuration["DOTNET_ENVIRONMENT"] = "Testing";

                    // WireMock URL passed to bot via environment variable
                    // (AppHost reads this and passes to bot as Discord:ApiBaseUrl)
                    settings.Configuration["Testing:DiscordApiBaseUrl"] = DiscordApi.Url!;
                });

        builder.Services.ConfigureHttpClientDefaults(cb =>
            cb.AddStandardResilienceHandler());

        App = await builder.BuildAsync();
        await App.StartAsync();

        // 3. Wait for services
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("hermod-api", cts.Token);
        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("hermod-bot", cts.Token);

        // 4. Extract connection strings
        _hermodDbConn = await App.GetConnectionStringAsync("hermod-db")
            ?? throw new InvalidOperationException("hermod-db not available");
        _authDbConn = await App.GetConnectionStringAsync("auth-db")
            ?? throw new InvalidOperationException("auth-db not available");
        _botDbConn = await App.GetConnectionStringAsync("bot-db")
            ?? throw new InvalidOperationException("bot-db not available");
    }

    public HttpClient CreateApiClient()
        => App.CreateHttpClient("hermod-api");

    public HermodContext CreateHermodDb()
        => new(new DbContextOptionsBuilder<HermodContext>()
            .UseNpgsql(_hermodDbConn).Options);

    public AuthDbContext CreateAuthDb()
        => new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(_authDbConn).Options);

    // Bot-db: raw SQL via Npgsql (avoids coupling to Hermod.Bot project)
    public NpgsqlConnection CreateBotDbConnection()
        => new(_botDbConn);

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        DiscordApi.Stop();
        DiscordApi.Dispose();
    }
}
```

---

## 9. Implementation Sequence

### Phase 1 — Foundation (P0)

1. **Bot Testing mode**: Add `Testing` environment check to `Hermod.Bot/Program.cs`:
   - Skip NetCord gateway + slash command registration
   - Register standalone `RestClient` with `Hostname` from `Discord:ApiBaseUrl` config (points to WireMock)
   - Skip gateway cache validation in `SharePlayToGroupHandler` (Option A from §4.3)
   - Keep Wolverine + NATS handlers unchanged
2. **API Testing mode**: Add conditional test-login endpoint (`/auth/test-login?userId=...`) when `ASPNETCORE_ENVIRONMENT=Testing`. Issues a real session cookie for the given UserId.
3. **AppHost Testing hook**: Pass `Testing:DiscordApiBaseUrl` to bot as `Discord__ApiBaseUrl` environment variable.
4. **Create `Hermod.E2E.Tests` project** with `AspireFixture`, `WireMockHelper`, `TestAuthHelper`, `DbHelper`, `AsyncWaiter`.
5. **Smoke tests** (§6.9): Validate AppHost boots, all resources healthy, API + Bot reachable.

### Phase 2 — Core Pipeline (P0)

6. **Upload-to-share tests** (§6.1): Single play, multi-play, duplicate, no-group, sharing-disabled scenarios.
7. **Group management tests** (§6.2): CRUD, membership, auth enforcement.

### Phase 3 — Cross-Process + Discord (P1)

8. **Community registration tests** (§6.3): Bot→API NATS flow via direct NATS publish.
9. **Enrollment tests** (§6.4): User enrollment lifecycle.
10. **Auth endpoint tests** (§6.6): Session management.
11. **Upload validation tests** (§6.7): Size limits, invalid files.
12. **Discord embed tests** (§6.10 tests 31-32): Verify WireMock receives correct embed payloads.

### Phase 4 — Extended Coverage (P2)

13. **Player claiming tests** (§6.5): Claim + backfill verification.
14. **Pagination tests** (§6.8): Multi-page play listing.
15. **Discord error handling tests** (§6.10 tests 33-35): 404 fallback, 403 DM failure, graceful degradation.

### Estimated test count

| Phase | Tests | Cumulative |
|-------|-------|------------|
| Phase 1 (Foundation + Smoke) | 3 | 3 |
| Phase 2 (Core Pipeline) | 11 | 14 |
| Phase 3 (Cross-Process + Discord) | 12 | 26 |
| Phase 4 (Extended + Error Handling) | 9 | 35 |

---

## Appendix A: Bot Testing Mode Implementation

The Bot's `Program.cs` needs a conditional split — gateway + slash commands in production, standalone RestClient + WireMock in testing:

```csharp
// In Hermod.Bot/Program.cs
var isTesting = builder.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    // PRODUCTION: Full NetCord gateway + slash commands + interaction handlers
    builder.Services
        .AddDiscordGateway(options =>
        {
            options.Intents = GatewayIntents.Guilds;
        })
        .AddApplicationCommands<SlashCommandInteraction, SlashCommandContext>()
        .AddApplicationCommands<MessageCommandInteraction, MessageCommandContext>()
        .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>()
        .AddGatewayHandlers(typeof(Program).Assembly);
}
else
{
    // TESTING: Standalone RestClient pointed at WireMock (no gateway, no slash commands)
    var discordApiHost = builder.Configuration["Discord:ApiBaseUrl"]
        ?? throw new InvalidOperationException("Discord:ApiBaseUrl required in Testing mode");
    builder.Services.AddSingleton(new RestClient(new RestClientConfiguration
    {
        Token = builder.Configuration["Discord:Token"] ?? "test-token",
        Hostname = discordApiHost,
    }));

    // No GatewayClient registered — handlers that use it must handle null/absent gracefully
}

// Wolverine + NATS is always registered (both modes)
builder.UseWolverine(opts => { ... });
```

**Handler adjustment** — `SharePlayToGroupHandler` gateway cache check:

```csharp
// Before (fails in Testing mode — no GatewayClient registered):
if (!gateway.Cache.Guilds.TryGetValue(mapping.DiscordGuildId, out var guild))

// After (gateway is optional in Testing mode):
public static async Task Handle(
    SharePlayToGroup message,
    BotDbContext db,
    GatewayClient? gateway,    // nullable — not registered in Testing mode
    RestClient rest,
    ILogger logger)
{
    // ... mapping lookup ...

    // Skip cache validation when gateway is not available (Testing mode)
    if (gateway is not null)
    {
        if (!gateway.Cache.Guilds.TryGetValue(mapping.DiscordGuildId, out var guild))
        {
            logger.LogWarning("Guild {GuildId} not in cache", mapping.DiscordGuildId);
            return;
        }
        if (!guild.Channels.TryGetValue(mapping.PostChannelId!.Value, out _))
        {
            logger.LogWarning("Channel {ChannelId} not in guild", mapping.PostChannelId.Value);
            return;
        }
    }

    // REST calls work in both modes (real Discord or WireMock)
    var sentMessage = await rest.SendMessageAsync(channelId, new MessageProperties { ... });
    // ...
}
```

This keeps production behavior unchanged while allowing Testing mode to skip the cache checks. The REST calls still execute against WireMock, testing serialization, URL construction, and error handling.

> **Wolverine nullable parameter injection**: Wolverine resolves handler method parameters from DI. If `GatewayClient` is not registered and the parameter is nullable (`GatewayClient?`), Wolverine injects `null`. Verify this works during Phase 1 implementation — if Wolverine throws, wrap in a try/catch or resolve from `IServiceProvider` instead.

## Appendix B: WireMockHelper Pattern

```csharp
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

public static class WireMockHelper
{
    /// <summary>
    /// Configure default Discord REST API stubs. Called once during fixture setup.
    /// </summary>
    public static void ConfigureDiscordStubs(WireMockServer server)
    {
        // POST /api/v10/channels/{id}/messages → send message
        server.Given(Request.Create()
                .UsingPost()
                .WithPath("/api/v10/channels/*/messages"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = "{{Random.Generate(18,\"[0-9]\")}}",
                    channel_id = "{{request.PathSegments.[2]}}",
                    content = "",
                    timestamp = "2026-01-01T00:00:00Z",
                })
                .WithTransformer());

        // PATCH /api/v10/channels/{id}/messages/{id} → edit message
        server.Given(Request.Create()
                .UsingPatch()
                .WithPath("/api/v10/channels/*/messages/*"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = "{{request.PathSegments.[4]}}",
                    channel_id = "{{request.PathSegments.[2]}}",
                    timestamp = "2026-01-01T00:00:00Z",
                })
                .WithTransformer());

        // POST /api/v10/users/@me/channels → create DM channel
        server.Given(Request.Create()
                .UsingPost()
                .WithPath("/api/v10/users/@me/channels"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = "999999999999999999",
                    type = 1,
                }));
    }

    /// <summary>
    /// Get all POST requests to /channels/*/messages (embed sends).
    /// </summary>
    public static IReadOnlyList<LogEntry> GetSendMessageRequests(WireMockServer server)
        => server.FindLogEntries(
            Request.Create().UsingPost().WithPath("/api/v10/channels/*/messages"));

    /// <summary>
    /// Get all PATCH requests to /channels/*/messages/* (embed edits).
    /// </summary>
    public static IReadOnlyList<LogEntry> GetEditMessageRequests(WireMockServer server)
        => server.FindLogEntries(
            Request.Create().UsingPatch().WithPath("/api/v10/channels/*/messages/*"));

    /// <summary>
    /// Reset request log between tests (stubs remain configured).
    /// </summary>
    public static void ResetRequestLog(WireMockServer server)
        => server.ResetLogEntries();
}
```

## Appendix C: AsyncWaiter Pattern

```csharp
public static class AsyncWaiter
{
    public static async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        string? message = null)
    {
        timeout ??= TimeSpan.FromSeconds(15);
        pollInterval ??= TimeSpan.FromMilliseconds(500);

        using var cts = new CancellationTokenSource(timeout.Value);
        while (!cts.Token.IsCancellationRequested)
        {
            if (await condition())
                return;
            await Task.Delay(pollInterval.Value, cts.Token);
        }
        throw new TimeoutException(
            message ?? "Condition was not met within the timeout period.");
    }
}
```

## Appendix D: BotDbContext Accessibility

`BotDbContext` is defined in `Hermod.Bot` (a Worker project). To query it from E2E tests, either:

1. **Extract `BotDbContext` into a shared library** (e.g., `Hermod.Bot.Data`) — cleanest but adds a project.
2. **Reference `Hermod.Bot` from E2E tests** — works but pulls in all bot dependencies (NetCord, etc.).
3. **Use raw SQL via Npgsql** — no type safety but zero coupling.

**Recommendation**: Option 1 if the E2E project needs rich querying; Option 3 for simple existence checks (e.g., `SELECT COUNT(*) FROM bot.play_posts WHERE play_id = @playId`). Start with Option 3 and extract to Option 1 if the test helper becomes unwieldy.

## Appendix E: CI Considerations

- Aspire.Hosting.Testing requires **Docker** (or Podman with Docker socket) on the CI agent — same as existing Testcontainers tests.
- E2E tests are slower (full process startup + container provisioning). Run them in a separate CI job or after fast tests pass.
- Consider a `[Category("E2E")]` TUnit attribute to allow filtering: `dotnet test --treenode-filter "/*/*/E2E/*"`.
- The `AspireFixture` is shared per session, so container startup cost is amortized across all ~30 tests.
- Budget ~2-3 minutes for full E2E suite (container startup: ~30s, API startup: ~10s, tests: ~60-90s).

## Appendix F: NetCord HTTPS / WireMock TLS

NetCord's `RestClient` builds URLs as `https://{Hostname}/api/v{version}`. Since WireMock defaults to HTTP, there's a protocol mismatch.

**Options (in order of preference)**:

1. **Custom `IRestRequestHandler`**: NetCord's `RestClientConfiguration.RequestHandler` accepts an `IRestRequestHandler` interface. Implement one that downgrades `https://` to `http://` before sending. This is the cleanest approach — 10 lines of code, no TLS complexity:
   ```csharp
   public class HttpDowngradeHandler : IRestRequestHandler
   {
       private readonly HttpClient _http = new();
       public void AddDefaultHeader(string name, IEnumerable<string> values)
           => _http.DefaultRequestHeaders.Add(name, values);
       public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
       {
           var uri = request.RequestUri!;
           request.RequestUri = new UriBuilder(uri) { Scheme = "http", Port = uri.Port }.Uri;
           return _http.SendAsync(request, ct);
       }
       public void Dispose() => _http.Dispose();
   }
   ```

2. **WireMock with `UseSSL = true`**: Start WireMock with a self-signed cert; configure the bot's `HttpClient` to trust all certs in Testing mode. More infrastructure but avoids the custom handler.

3. **WireMock.Net.Aspire container with HTTPS**: The Aspire container integration can be configured with TLS. Heaviest approach but most isolated.

**Recommendation**: Option 1. It's trivial to implement and avoids any TLS certificate management in CI.
