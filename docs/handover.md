# Hermod Bot — Handover

_Last updated: 2026-02-25 (claim player)_

## Current State

Hermod is a play-sharing platform for board games recorded in the BGStats app. The rewrite has a working end-to-end pipeline from upload through to Discord embed posting, with Discord OAuth authentication, a separated Bot/API architecture communicating via Core NATS, and a SvelteKit web frontend served through a YARP reverse proxy gateway.

### What Works

- **BGStats parsing** — `.bgsplay` files are parsed into structured play data (multi-play, scoresheet, expansion support)
- **Authenticated uploads** — `POST /api/plays/upload` accepts `.bgsplay` files from authenticated users (Discord OAuth, cookie sessions)
- **Upsert pipeline** — re-uploading the same play (by BgStatsPlayUuid + user) updates the existing record instead of creating duplicates
- **Per-group fan-out** — after a play is persisted, `SharePlayToGroup` events are emitted for every group the uploader belongs to (where sharing is enabled)
- **Cross-process messaging** — Bot ↔ API communicate via Wolverine Core NATS transport (at-most-once, in-memory). API listens on `hermod.api`, Bot listens on `hermod.bot`
- **Auth infrastructure** — Discord OAuth with JIT user provisioning, dual-database design (auth-db + hermod-db), cookie sessions
- **Play embeds** — `SharePlayToGroupHandler` receives `SharePlayToGroup` messages, builds Discord embeds via `PlayEmbedBuilder`, posts to the configured channel. New plays → new embed message, updated plays → edit existing. `PlayPostEntity` tracks posted messages (unique per PlayId + GroupId) for idempotent edit-on-update
- **`/sharing` admin commands** — `SharingModule` provides `/sharing channel` (set the embed target channel for a guild) and `/sharing toggle` (enable/disable play sharing for a guild)
- **Guild join/leave → auto group creation** — when the bot joins a Discord server (or on ready sync), it registers a core `GroupEntity` via `RegisterGuild` → `api-inbox`. Deterministic GroupId (UUID v5 from DiscordGuildId) ensures idempotent upserts. Leaving a guild deactivates the mapping and disables sharing via `UpdateGroupSharing`
- **BotDbContext** — separate EF Core context (schema: `bot`) with `GuildMappingEntity` linking Discord guilds to core Groups; auto-migrated at startup
- **Group enrollment (`/enroll`)** — Discord slash command lets guild members join their play-sharing group. Registered users are enrolled immediately via `EnrollInGroup` → `api-inbox`. Unregistered users get a link to the web app that takes them through Discord OAuth and auto-enrolls them on the `/enroll` page. `PUT /api/groups/{groupId}/membership` is the idempotent web endpoint (201 Created / 204 No Content / 401 / 404)
- **Player claiming** — "Claim Player" message command on play embeds lets users self-identify as a BGStats player. Select menu filters out already-linked players (`MappedUserId is null`). API handler creates `PlayerMappingEntity`, sets `MappedUserId` on the target play, and backfills all other plays with the same UUID. Uploader is blocked from claiming (API guard returns `IsUploader` since they're auto-linked via `meRefId`)
- **InteractionService** — Discord.Net slash command infrastructure wired up (`InteractionHandler` hosted service, global command registration)
- **End-to-end upload tested** — full pipeline verified against running Aspire app with repeated uploads
- **Web frontend** — SvelteKit app (`adapter-static`) with pages for landing, dashboard, plays, groups, and upload; Discord OAuth login working through the gateway
- **YARP gateway** — reverse proxy in front of the frontend; in dev mode proxies all traffic to Vite (which handles API routing via its proxy config); in publish mode routes `/api`, `/auth`, `/signin-discord` to the API directly and serves static files
- **Forwarded headers** — API uses `UseForwardedHeaders()` so OAuth redirect URIs are correct behind YARP
- **pgAdmin** — available in Aspire dashboard for Postgres debugging

### Pipeline Architecture

```
POST /api/plays/upload (authenticated)
  → UploadPlays endpoint (validate, parse, persist UploadEntity)
    → PlayFileUploaded (per upload)
      → ExtractPlaysHandler (LoadAsync + HandlerContinuation tuple)
        → PlayExtracted
          → PlayExtractedHandler (UPSERT by BgStatsPlayUuid + UploadedById)
            → PlayPersisted(PlayId, UploadedById, Created|Updated)
              → SharePlayHandler (fan-out per group with AllowSharing)
                → SharePlayToGroup(PlayId, GroupId, ChangeType, Snapshot) → hermod.bot NATS subject
                  → SharePlayToGroupHandler (Bot)
                    → PlayEmbedBuilder → Discord embed posted/edited
                    → PlayPostEntity upserted (tracks message ID for edit-on-update)
      → DistributeFileHandler (LoadAsync + HandlerContinuation tuple, parallel)
        → DistributePlayFile(UploadId, PlayerUuid) — no consumer yet
```

- **MultipleHandlerBehavior.Separated** — `ExtractPlaysHandler` and `DistributeFileHandler` each get their own local queue for independent processing of `PlayFileUploaded`
- **LoadAsync compound handler pattern** — uses `(HandlerContinuation, UploadEntity?)` tuple return to skip `Handle` when entity is null (plain `T?` return passes null through — see `memory/wolverine-compound-handlers.md`)
- **Retry policy** — `DbUpdateConcurrencyException` retries with exponential backoff (50/100/250ms) for race conditions when the same play is upserted concurrently
- All handlers are static, use Wolverine cascades (return the next message), and rely on `AutoApplyTransactions()` — no manual `SaveChangesAsync()`

### Data Model

```
UserProfileEntity (1) ─── (*) UserGroupEntity (*) ─── (1) GroupEntity
    │                                                       (AllowSharing)
    │ UploadedById (required)
    ▼
PlayEntity
    │  Unique: (BgStatsPlayUuid, UploadedById)
    │  Players
    ▼
PlayPlayerEntity (*) ───── (0..1) UserProfileEntity
                                  (MappedUserId, nullable)

UserProfileEntity (1) ─── (*) PlayerMappingEntity
                                (BgStatsPlayerUuid → MappedUserId)

UploadEntity (1) ─── (*) PlayEntity
    │                     (UploadId, nullable)
    └── FileContent (jsonb), UploadedById (required)
```

Key design:
- **No GroupId on PlayEntity** — plays are not tied to a single group; fan-out happens via `SharePlayHandler`
- **UploadedById is required** (non-nullable) on both `UploadEntity` and `PlayEntity`
- **Unique index** on `(BgStatsPlayUuid, UploadedById)` enables upsert
- **Dual auth databases** — `AuthDbContext` (schema: auth user + external logins) and `HermodContext` (app data), both keyed by the same UserId

### Completed Work

| Phase | Delivered |
|-------|-----------|
| Phase 1: Foundation | Solution structure, BGStats parser, Data layer, API skeleton, 14 TUnit tests |
| Phase 2–3: Upload pipeline | Wolverine event cascade, upload endpoint, play extraction, embed posting (colocated) |
| Discord Separation | Bot split into separate process (`Hermod.Bot`), Wolverine transport, slash commands, guild sync |
| NATS Transport Migration | Migrated Bot ↔ API from Wolverine PostgreSQL queues to Core NATS subjects (`hermod.api`, `hermod.bot`) |
| Phase 4: Auth (partial) | AuthDbContext, Discord OAuth, cookie sessions, test auth handler, authenticated upload endpoint |
| Play Pipeline Rework | Upsert, GroupId removal, per-group fan-out via `SharePlayToGroup`, `Hermod.Messages` shared project |
| Bot + Aspire wiring | `Hermod.Bot` added to solution, AppHost, Aspire orchestration; `PlaySnapshot` in messages |
| Play embeds | `SharePlayToGroupHandler` (real implementation), `PlayEmbedBuilder`, `PlayPostEntity`, `/sharing` admin commands (channel + toggle), edit-on-update for re-uploaded plays |
| Handler fixes | LoadAsync with HandlerContinuation tuple, MultipleHandlerBehavior.Separated, DbUpdateConcurrencyException retry, DatePlayed UTC fix |
| Web frontend | SvelteKit app with adapter-static, Vite proxy for dev, YARP gateway for publish, Discord OAuth login, play/group/upload pages |
| Guild join/leave | `GuildEventService` (Discord.Net + Discord.Addons.Hosting), `RegisterGuildHandler`/`UpdateGroupSharingHandler` on API, `BotDbContext` with `GuildMappingEntity`, deterministic GroupId via UUID v5, idempotent upserts on both sides |
| Group enrollment | `/enroll` slash command, `EnrollInGroupHandler` (API), `PUT /api/groups/{groupId}/membership` endpoint, `InteractionHandler` service, web auto-enroll page, `joinGroup()` API function |
| Player claiming | "Claim Player" message command, `ClaimPlayerHandler` (API), `ClaimPlayerModule` (Bot), select menu with `MappedUserId` filter, uploader guard, backfill across plays |

### Test Coverage

- **14 BGStats parser tests** — parsing, multi-play, score expressions
- **10 handler unit tests** — `PlayExtractedHandler` (create, update, cross-user, player mappings) + `SharePlayHandler` (fan-out filtering, change type)
- **9 claim player handler unit tests** — `ClaimPlayerHandler` (claim, mapping creation, MappedUserId set, backfill, already claimed, uploader blocked, not registered, unknown UUID, no overwrite)
- **8 enrollment handler unit tests** — `EnrollInGroupHandler` (enroll, already member, not registered, group not found, member role, no duplicates, multiple users)
- **11 guild handler unit tests** — `RegisterGuildHandler` (create, upsert, deterministic ID, re-enable sharing) + `UpdateGroupSharingHandler` (enable, disable, no-op)
- **9 integration tests** — auth endpoints, upload endpoint (auth/unauth/oversized/invalid/persistence), group endpoints
- **6 group endpoint integration tests** — group creation, round-trip, 404, membership (created/idempotent/unauth/not-found/appears-in-list)
- All tests use **TUnit** with `[ClassDataSource]` property injection; integration tests use Testcontainers (Podman)
- **63 total tests passing** (unit tests; integration tests require Podman)

---

## Solution Structure

```
Hermod.slnx
├── src/
│   ├── Hermod.AppHost/          # Aspire orchestrator
│   ├── Hermod.ServiceDefaults/  # Shared Aspire config
│   ├── Hermod.Messages/         # Shared message records (crosses process boundaries)
│   │   ├── SharePlayToGroup     # PlayId, GroupId, ChangeType, PlaySnapshot
│   │   ├── PlaySnapshot         # Denormalized play data for Bot consumption
│   │   └── PlayerSnapshot       # Per-player data within a snapshot
│   ├── Hermod.Api/              # ASP.NET API — platform-agnostic data gateway
│   │   ├── Endpoints/           # Wolverine HTTP endpoint handlers
│   │   ├── Handlers/            # Wolverine message handlers
│   │   └── Messages/            # Api-internal message records
│   ├── Hermod.Bot/              # Discord bot — separate Worker process
│   │   ├── Data/                # BotDbContext, GuildMappingEntity, PlayPostEntity (schema: bot)
│   │   ├── Embeds/              # PlayEmbedBuilder (Discord embed construction)
│   │   ├── Handlers/            # Wolverine message handlers (SharePlayToGroupHandler)
│   │   ├── Migrations/          # EF Core migrations for BotDbContext
│   │   ├── Modules/             # Discord.Net interaction modules (EnrollModule, SharingModule, ClaimPlayerModule)
│   │   ├── Services/            # GuildEventService, InteractionHandler
│   │   └── Program.cs           # Host builder, Discord.Net, InteractionService, Wolverine config
│   ├── Hermod.Data/             # EF Core entities, HermodContext, config, migrations
│   ├── Hermod.BGStats/          # .bgsplay file parsing (standalone, no DI)
│   └── Hermod.Web/              # SvelteKit frontend (adapter-static, Vite + TypeScript)
├── tests/
│   ├── Hermod.Api.Tests/        # Handler unit tests + integration tests
│   └── Hermod.BGStats.Tests/    # Parser tests
└── legacy/                      # Old .NET 6 code (reference only)
```

### Cross-Process Message Flow

```
Hermod.Messages (shared project, no DI deps)
  ├── PlayChangeType         enum (Created, Updated)
  ├── SharePlayToGroup       record (PlayId, GroupId, ChangeType, PlaySnapshot)
  ├── PlaySnapshot           record (GameName, DatePlayed, Duration, ...)
  ├── PlayerSnapshot         record (PlayerName, Score, CalculatedScore, Winner, ...)
  ├── RegisterGuild          record (DiscordGuildId, GuildName) — Bot → API
  ├── GuildRegistered        record (GroupId, DiscordGuildId) — API → Bot (response)
  ├── UpdateGroupSharing     record (GroupId, AllowSharing) — Bot → API
  ├── EnrollInGroup          record (DiscordId, GroupId) — Bot → API
  ├── EnrollmentResult       record (Status) — API → Bot (response)
  ├── EnrollmentStatus       enum (Enrolled, AlreadyMember, NotRegistered, GroupNotFound)
  ├── ClaimPlayer            record (DiscordId, BgStatsPlayerUuid, PlayId) — Bot → API
  ├── ClaimPlayerResult      record (Status, PlayerName?) — API → Bot (response)
  ├── ClaimPlayerStatus      enum (Claimed, AlreadyClaimed, IsUploader, NotRegistered, PlayerNotFound)
  └── GroupIdFactory         static (ForDiscordGuild → deterministic UUID v5)

Hermod.Api
  opts.ListenToNatsSubject("hermod.api")              ← RegisterGuild, UpdateGroupSharing, EnrollInGroup, ClaimPlayer
  opts.PublishMessage<SharePlayToGroup>().ToNatsSubject("hermod.bot")

Hermod.Bot
  opts.ListenToNatsSubject("hermod.bot")               ← SharePlayToGroup
  opts.PublishMessage<RegisterGuild>().ToNatsSubject("hermod.api")
  opts.PublishMessage<UpdateGroupSharing>().ToNatsSubject("hermod.api")
  opts.PublishMessage<EnrollInGroup>().ToNatsSubject("hermod.api")
  opts.PublishMessage<ClaimPlayer>().ToNatsSubject("hermod.api")
  → SharePlayToGroupHandler (builds embed, posts/edits in Discord channel, tracks in PlayPostEntity)
  → GuildEventService (guild join/leave/ready sync)
  → InteractionHandler (slash command registration + dispatch)
  → EnrollModule (/enroll slash command)
  → SharingModule (/sharing channel, /sharing toggle)
  → ClaimPlayerModule ("Claim Player" message command + select menu handler)
```

---

## How to Run

### Prerequisites

- .NET 10 SDK (10.0.103)
- Aspire CLI: `~/.aspire/bin/aspire`
- Podman (for integration tests and Aspire dev containers)

### Discord Setup

1. Create app at https://discord.com/developers/applications
2. Enable privileged intents: **Server Members** + **Message Content**
3. Add OAuth2 redirect URIs: `https://localhost:7007/signin-discord` (direct API) and `http://localhost:8080/signin-discord` (via YARP gateway)
4. Set user secrets on AppHost:

```bash
dotnet user-secrets set "Parameters:discord-token" "<token>" --project src/Hermod.AppHost
dotnet user-secrets set "Parameters:discord-client-id" "<id>" --project src/Hermod.AppHost
dotnet user-secrets set "Parameters:discord-client-secret" "<secret>" --project src/Hermod.AppHost
```

### Build & Run

```bash
dotnet build Hermod.slnx -verbosity:quiet
~/.aspire/bin/aspire run --project src/Hermod.AppHost/Hermod.AppHost.csproj
dotnet test --solution Hermod.slnx
```

---

## Next Steps

### Immediate: Player linking polish

Auto-link via `meRefId` and self-claim via message command are implemented. Remaining:

- **Manual tagging**: Slash command or web UI for admins to map players who haven't self-claimed
- **@-mentions in embeds**: Update `PlayEmbedBuilder` to mention linked users in the player list
- **Claim notification**: Optionally notify the uploader when someone claims a player in their play

### Pending: DistributePlayFile handler

`DistributeFileHandler` emits `DistributePlayFile(UploadId, PlayerUuid)` for each non-uploader player in the file, but no handler consumes it yet. This is for DM-ing linked players their results.

### Pending: Multi-play threshold (anti-spam)

`GroupEntity.SpamThreshold` (if added) would control when a single upload with many plays posts a summary embed instead of individual embeds. `SharePlayHandler` currently emits one `SharePlayToGroup` per play per group with no batching.

### Pending: Web frontend polish

SvelteKit frontend is scaffolded with pages for landing, dashboard, plays, groups, and upload. Auth (Discord OAuth login/logout) is working. Remaining work:

- Wire up actual API calls on plays/groups/upload pages (currently placeholder UI)
- Error handling and loading states
- Responsive design / mobile layout
- Player linking UI (map BGStats players to platform users)

---

## Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| Plays not tied to groups | A play belongs to a user; sharing is a separate concern handled by fan-out |
| Upsert by (UUID, user) | Same play re-uploaded by the same user updates in place; different users uploading the same play create separate records |
| Dual databases | Auth (identity-only) separated from app data; no cross-database FK, linked by UserId |
| Wolverine Core NATS transport | Bot ↔ API messaging via Core NATS (at-most-once, in-memory); `hermod.api` and `hermod.bot` subjects. No JetStream — simplicity over durability for this use case |
| Cascade return values | Handlers return the next message type (or `OutgoingMessages` for fan-out); no manual `IMessageBus.PublishAsync` |
| LoadAsync + HandlerContinuation | Compound handlers use `(HandlerContinuation, T?)` tuple return to skip Handle on null; plain `T?` passes null through |
| MultipleHandlerBehavior.Separated | Multiple handlers for the same message type get independent local queues |
| PlaySnapshot in SharePlayToGroup | Denormalized play data avoids round-trip from Bot to API; acceptable staleness for updates |
| Retry on DbUpdateConcurrencyException | Exponential backoff (50/100/250ms) handles race when same play upserted concurrently from rapid re-uploads |
| Explicit Vogen FK init | `PlayPlayerEntity.PlayId` must be set explicitly when creating child entities — Vogen throws on default struct hash |
| `Hermod.Messages` project | Cross-process message types live here (no DI deps); Api and Bot both reference it |
| YARP gateway (publish only for API) | In dev, Aspire DCP binds proxies to 127.0.0.1 only — containers can't reach host projects. YARP catch-all routes to Vite, which proxies API traffic. In publish, YARP routes API traffic directly |
| SvelteKit with adapter-static | SPA fallback (`200.html`) — all routing handled client-side; YARP serves static files in production |
| UseForwardedHeaders | Required behind YARP so OAuth middleware constructs correct redirect URIs from X-Forwarded-Host/Proto |
| Deterministic GroupId (UUID v5) | GroupId derived from DiscordGuildId via UUID v5 (SHA-1 + namespace GUID). Same guild always maps to same group; no Discord-specific fields in core data model. Each future provider gets its own namespace GUID — no collision risk (v5 and v4 occupy disjoint UUID space) |
| Guild registration via InvokeAsync | Bot uses `InvokeAsync<GuildRegistered>` (10s timeout) for synchronous request-response via NATS. API upserts GroupEntity and returns GroupId. Bot upserts GuildMappingEntity |
| BotDbContext (schema: bot) | Separate EF context for Discord-specific data; lives in `Hermod.Bot/Data/`. Auto-migrated at startup. `GuildMappingEntity` has unique index on `DiscordGuildId` (stored as `numeric(20,0)`) |

---

## Known Gotchas

- **Wolverine LoadAsync null handling**: `LoadAsync` returning `T?` does NOT skip `Handle` — must return `(HandlerContinuation, T?)` tuple. See `memory/wolverine-compound-handlers.md` for full details
- **Wolverine `[Entity]` + Vogen IDs**: `[Entity]` attribute can't auto-convert plain `Guid` in messages to Vogen wrapper types on entities. Use inline `FindAsync` or `LoadAsync` instead
- **`[Required]` from DataAnnotations**: Does NOT make Wolverine add null guards on handler parameters
- **Vogen + EF Core InMemory**: Child entities with Vogen FK structs must have the FK explicitly set before `db.Add()` — the default struct throws on `GetHashCode()` during graph traversal
- **Wolverine IFormFile + DbContext**: Having `DbContext` as a direct method parameter on `[WolverinePost]` breaks multipart/form-data (415). Resolve from `HttpContext.RequestServices` instead
- **Wolverine HTTP empty-body + DbContext**: `[WolverinePut]`/`[WolverineDelete]` with no request body will try to deserialize the empty body as `DbContext` if it's a direct parameter. Use `[FromServices]` attribute on the `DbContext` parameter
- **Wolverine multiple DbContext types in message handler**: `AutoApplyTransactions` fails at startup if a handler method signature includes two DbContext types. Resolve the read-only DbContext via `IServiceProvider` instead of direct injection
- **Wolverine HTTP compound handlers**: Only `Before`/`After` methods work (not `Validate`/`Load`); `LoadAsync` is for message handlers only
- **TUnit `HasCount()`**: Deprecated — use `Count().IsEqualTo()` instead
- **Podman socket**: Must be started before integration tests: `systemctl --user start podman.socket`
- **Wolverine `InvokeAsync<T>` over NATS**: Use explicit `timeout: TimeSpan.FromSeconds(10)` for cross-process request-response. Unhandled `TimeoutException` in a `BackgroundService` crashes the host — always wrap in try-catch
- **Architecture doc** (`docs/architecture.md`): Significantly stale — references SQLite, Hermod.Core, Hermod.Contracts, and a colocated Bot design that no longer exist. Should be rewritten to match current reality.
- **Debug generated code**: Run `dotnet run -- codegen write` to dump Wolverine's generated handler code for debugging compound handler wiring
- **Aspire DCP + Podman containers**: DCP binds all proxy ports to `127.0.0.1`, so containers (YARP, pgAdmin) cannot reach host-based projects via `host.containers.internal`. This is why YARP only routes API traffic in publish mode — in dev, Vite's proxy (running on the host) handles it instead.
- **YARP gateway ports**: Configured as `WithHostPort(8080)` / `WithHostHttpsPort(8443)`. Access the app at `http://localhost:8080` in dev.
