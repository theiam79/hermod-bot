# Hermod Platform Architecture

> This document describes the architecture for the Hermod platform rewrite. It is intended for external review and feedback.

## 1. Overview

Hermod is evolving from a single-server Discord bot into a **play-sharing platform** with Discord as one client. The platform allows users to upload board game play data recorded in the [BGStats](https://www.bgstatsapp.com/) app, share results with groups, and view play history.

### Why Rewrite?

| Problem | Impact |
|---------|--------|
| .NET 6 (EOL), MediatR/AutoMapper indirection, no tests | Maintenance burden, security risk |
| BGStats files no longer contain BGG usernames | Player linking is broken — must redesign |
| Files can contain multiple plays | Old code only processes the first play |
| Single-server Discord bot design | Can't scale to multiple servers or non-Discord clients |
| Discord policy changes | Need to share plays outside Discord as a fallback |
| Player names are PII | Files shouldn't be publicly downloadable |

### Goals

- Modern .NET 10 stack with Aspire for local dev orchestration
- API-first: single data gateway serving Discord bot, web frontend, and future clients
- Multi-group support (Discord guilds initially, extensible beyond Discord)
- Web frontend for play management (Vite + React SPA)
- Future: PWA with share intent for mobile uploads

---

## 2. Architecture Diagram

```
┌──────────────────────────────────────────────────────────┐
│                    Hermod.AppHost                         │
│                  (Aspire Orchestrator)                    │
│                                                          │
│  ┌─────────────┐   ┌─────────────┐   ┌──────────────┐   │
│  │ Hermod.Api   │   │ Hermod.Bot  │   │ Hermod.Web   │   │
│  │ (ASP.NET     │   │ (Discord    │   │ (Vite+React  │   │
│  │  Minimal API)│   │  Worker)    │   │  SPA)        │   │
│  └──────┬───────┘   └──────┬──────┘   └──────┬───────┘   │
│         │                  │                 │            │
│   ┌─────┴──────┐           │ HTTP            │ HTTP       │
│   │Hermod.Core │           │ (API key)       │ (cookies)  │
│   │(Biz Logic) │           │                 │            │
│   └─────┬──────┘           ▼                 ▼            │
│   ┌─────┴──────┐     ┌────────────────────────────┐      │
│   │Hermod.Data │     │       Hermod.Api            │      │
│   │(EF + SQLite)│    │   (single data gateway)     │      │
│   └────────────┘     └────────────────────────────┘      │
│                                                          │
│   Shared Libraries (no framework dependencies):          │
│   ┌──────────────────┐  ┌──────────────────┐             │
│   │ Hermod.BGStats   │  │ Hermod.Contracts │             │
│   │ (Play Parsing)   │  │ (Shared DTOs)    │             │
│   └──────────────────┘  └──────────────────┘             │
└──────────────────────────────────────────────────────────┘
```

### Dependency Graph

```
Bot  → Contracts, BGStats, Discord.Net       (NO reference to Core or Data)
API  → Core → Data, Contracts, BGStats
Web  → (standalone Vite SPA, calls API over HTTP)
```

### Why Bot → API over HTTP (not Bot → Core directly)?

This was a deliberate architectural decision driven by:

1. **SQLite write contention**: SQLite has a single-writer limitation. If both Bot and API write to the same database from separate processes, writes will contend. Funneling all writes through the API avoids this entirely.
2. **Single API contract**: One set of endpoints for all clients (Bot, Web, future PWA/mobile). No divergent code paths.
3. **Clean separation**: Bot is a thin Discord client. It parses files locally and delegates all data operations to the API. It has no dependency on Core or Data.
4. **Aspire makes it cheap**: Service discovery, health checks, and resilience policies are provided by Aspire with minimal configuration. The HTTP hop is negligible for this workload.
5. **Operational simplicity**: One process owns the database. Debugging, migration, and scaling decisions are straightforward.

### Wolverine.Fx Strategy

API endpoints are implemented using **Wolverine.Http** (`[WolverineGet]`, `[WolverinePost]`, etc.) instead of raw Minimal API `MapGet`/`MapPost`. This gives us:

- **Command/handler pattern** with source-generated wiring (no MediatR indirection)
- **Built-in middleware** for validation, error handling, and retry policies
- **Future messaging path**: The same handler methods can serve as both HTTP endpoints and async message handlers. When the time comes, we can add a transport (e.g., RabbitMQ) for Bot→API resilience without rewriting handlers.

---

## 3. Project Structure

| Project | Type | Purpose | References |
|---------|------|---------|------------|
| `Hermod.AppHost` | Aspire Host | Orchestrates all services, injects config | API, Bot, Web |
| `Hermod.ServiceDefaults` | Class Library | Shared Aspire config (OTel, health, resilience) | — |
| `Hermod.Api` | ASP.NET Minimal API | Single data gateway, all HTTP endpoints | Core, Contracts |
| `Hermod.Bot` | Worker Service | Discord bot, thin client | Contracts, BGStats |
| `Hermod.Web` | Vite + React SPA | Web frontend | (calls API over HTTP) |
| `Hermod.Core` | Class Library | Business logic, services | Data, BGStats |
| `Hermod.Data` | Class Library | EF Core entities, DbContext, config | — |
| `Hermod.BGStats` | Class Library | .bgsplay file parsing (standalone) | — |
| `Hermod.Contracts` | Class Library | Shared request/response DTOs, typed HTTP client | — |

---

## 4. Authentication & Authorization

### User Auth (Browser → API)

**Approach**: ASP.NET Core built-in OAuth middleware with the BFF (Backend for Frontend) pattern.

- **Identity provider**: Discord OAuth 2.0 (via `AspNet.Security.OAuth.Discord`)
- **Session management**: Encrypted, server-side cookies
- **Cookie security**: `HttpOnly = true`, `Secure = true`, `SameSite = Lax`
- **No CORS needed**: SPA is served from the same origin as the API (BFF pattern)
- **CSRF protection**: ASP.NET Core built-in anti-forgery tokens (not legacy `X-Requested-With` header check)
- **API behavior**: Returns `401`/`403` status codes for unauthorized requests (no redirects on `/api/*` routes)
- **Extensible**: Adding OAuth providers (Google, GitHub) is a NuGet package + config change

**What we store locally**: DiscordId, display name, preferences. No passwords, no tokens persisted beyond the session.

### Service-to-Service Auth (Bot → API)

- **Pre-shared API key** injected via Aspire parameters at startup
- Bot sends `Authorization: ApiKey <key>` on every request
- API validates the key and creates a service-identity claims principal
- No OAuth client credentials flow — overkill for single-deployment, same-host services

### Security Hardening

| Measure | Implementation |
|---------|---------------|
| Rate limiting | ASP.NET Core rate limiting middleware on auth and upload endpoints |
| Security headers | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin` |
| Data Protection keys | File-based or SQLite-based persistence (not ephemeral) |
| No JWTs for sessions | Cookies are simpler, revocable, and don't leak claims to the client |

### What We Explicitly Avoid

- **JWTs for browser sessions** — cookies are more secure for this use case
- **SPA handling OAuth directly** — the BFF pattern keeps tokens server-side
- **External auth services** (Auth0, Keycloak) — unnecessary complexity for a small user base, ASP.NET Core handles it natively
- **OAuth client credentials for Bot** — a pre-shared API key is simpler and sufficient for same-host deployment

---

## 5. Data Model

All IDs use **Vogen** value objects wrapping `Guid` in the data layer. The Contracts layer uses plain `Guid` to avoid coupling clients to Vogen.

### Entity Relationship Diagram

```
UserEntity (1) ──── (*) UserGroupEntity (*) ──── (1) GroupEntity
    │                                                    │
    │ UploadedPlays                                      │
    ▼                                                    │
PlayEntity (*) ──────────────────────────────────────────┘
    │                                          (GroupId, nullable)
    │ Players
    ▼
PlayPlayerEntity (*) ───── (0..1) UserEntity
                                  (MappedUserId, nullable)

UserEntity (1) ──── (*) PlayerMappingEntity
                         (OwnerUserId → MappedUserId)
```

### Key Entities

| Entity | Purpose | Notable Fields |
|--------|---------|----------------|
| `UserEntity` | Platform user | `DiscordId` (nullable — future non-Discord users), `BggId`/`BggUsername` (optional) |
| `GroupEntity` | Play-sharing group | `DiscordGuildId`/`DiscordPostChannelId` (nullable), `AllowSharing` |
| `UserGroupEntity` | Many-to-many with role | Composite PK `(UserId, GroupId)`, `Role` enum (Member, Admin) |
| `PlayEntity` | A recorded board game play | `BgStatsPlayUuid` (dedup), `RawPlayFileJson` (for re-sharing), `GameName`, `DatePlayed` |
| `PlayPlayerEntity` | Player in a play | `BgStatsPlayerUuid`, `PlayerName`, `MappedUserId` (nullable — resolved platform user), `Score`, `Winner` |
| `PlayerMappingEntity` | Per-owner UUID→user mapping | Unique `(OwnerUserId, BgStatsPlayerUuid)` — see Player Linking below |

---

## 6. Player Linking Strategy

BGStats player UUIDs are **per-installation** — the same real person has different UUIDs in different users' BGStats databases. BGG usernames are no longer included in export files.

### How It Works

1. **Registration**: Users register on the platform via Discord OAuth
2. **Player mapping table**: `PlayerMapping(OwnerUserId, BgStatsPlayerUuid, MappedUserId)`
   - When user A uploads a file, each player UUID (from A's BGStats installation) can be mapped to a platform user
   - Mappings are per-owner — user A's UUID for "Tyler" differs from user B's UUID for "Tyler"
3. **First upload**: After uploading, the uploader is prompted to tag which platform users correspond to which players in the file
4. **Auto-mapping**: Once a mapping exists for (owner + playerUuid), future uploads auto-resolve
5. **Self-identification**: `userInfo.meRefId` in the file identifies the recording user — auto-map on first upload
6. **Fallback**: Group members see all shared plays (no player-level filtering until mappings exist)

---

## 7. .bgsplay File Format

Files exported from BGStats use the `.bgsplay` extension and contain JSON:

```
Root
├── about: string
├── userInfo: { meRefId: int }
├── players[]: { uuid, id, name, isAnonymous }
│   NOTE: No bggUsername field (removed in recent BGStats versions)
├── locations[]: { uuid, id, name }
├── games[]: { uuid, id, name, bggId, cooperative, highestWins,
│              noPoints, usesTeams, urlThumb, ... }
└── plays[]: { uuid, playDate, durationMin, usesTeams, comments,
               scoresheet (JSON string), gameRefId, locationRefId,
               playerScores[]: { playerRefId, score, winner, rank,
                                 role, team, metaData: { scoreUuid } },
               expansionPlays[]: { gameRefId } }
```

**Key characteristics:**
- A file can contain **multiple plays** (1–6 observed in samples)
- `scoresheet` is a JSON string with rich category scoring (VP track, Cards, etc.)
- `playerScores[].metaData.scoreUuid` maps to player UUIDs in the scoresheet
- `expansionPlays` links to expansion games used during a play

---

## 8. API Endpoints

### Service Endpoints (API key auth — used by Bot)

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/plays/upload` | Upload parsed plays from a .bgsplay file |
| `GET` | `/api/plays?groupId=` | List plays (filtered) |
| `GET` | `/api/plays/{id}` | Play detail with players |
| `POST` | `/api/users` | Create/register user (by Discord ID) |
| `GET` | `/api/users/by-discord/{discordId}` | Lookup user by Discord ID |
| `GET` | `/api/users/{id}` | User profile |
| `PUT` | `/api/users/{id}` | Update user preferences |
| `POST` | `/api/groups` | Create group (from Discord guild) |
| `GET` | `/api/groups/by-discord/{guildId}` | Lookup group by Discord guild ID |
| `GET` | `/api/groups/{id}` | Group detail |
| `PUT` | `/api/groups/{id}/settings` | Update group settings |
| `POST` | `/api/groups/{id}/members` | Add user to group |
| `POST` | `/api/player-mappings` | Create/update player mapping |
| `GET` | `/api/player-mappings?ownerId=` | Get mappings for an owner |

### User Endpoints (Cookie auth — used by Web frontend)

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/auth/login` | Initiate Discord OAuth flow |
| `POST` | `/auth/logout` | End session |
| `GET` | `/auth/me` | Current session info |
| `GET` | `/api/users/me` | Current user profile |
| `PUT` | `/api/users/me` | Update own preferences |
| `GET` | `/api/groups` | User's groups |
| `POST` | `/api/player-mappings` | Manage own mappings |

---

## 9. Privacy Considerations

- **Raw .bgsplay files contain real names** — files are not publicly accessible
- **Play embeds show player names** — acceptable in private group contexts (Discord channels, authenticated web views)
- **All API endpoints require authentication** — no anonymous data access
- **File re-sharing**: Only with group members, not publicly downloadable
- **Future**: Option to use display names instead of real names in embeds

---

## 10. Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10 | Runtime and framework |
| ASP.NET Core Minimal API | 10 | HTTP API (endpoint routing via Wolverine.Http) |
| Wolverine.Fx | 5.x | Command/handler pattern, HTTP endpoints, future async messaging |
| Aspire | 13.x | Local dev orchestration, service defaults |
| EF Core + SQLite | 10 | Data access (extensible to Postgres) |
| Discord.Net | Latest | Discord bot framework |
| Vogen | 8.x | Strongly-typed value object IDs |
| Mapperly | 4.x | Source-generated object mapping |
| TUnit | Latest | Testing framework |
| NCalc | 5.x | Score expression evaluation |
| Vite + React + TypeScript | Latest | Web frontend SPA |
| ASP.NET Core OAuth + AspNet.Security.OAuth.Discord | — | Authentication |

### What We Dropped (and Why)

| Dropped | Reason |
|---------|--------|
| MediatR | Unnecessary indirection for this scale — plain services with DI |
| AutoMapper | Runtime reflection — replaced by Mapperly (compile-time source generation) |
| FluentValidation | Not needed — ASP.NET Core model validation is sufficient |
| FluentResults | Not needed — standard exceptions and result patterns |
| MediatR-style indirection | Wolverine.Fx command/handler pattern replaces ad-hoc service classes where appropriate |

---

## 11. Implementation Phases

| Phase | Scope | Status |
|-------|-------|--------|
| **1. Foundation** | Solution structure, BGStats parser, Data layer, Core services, API skeleton, tests | **Complete** |
| **2. Contracts + API + Service Auth** | Shared DTOs, all API endpoints, API key auth, typed HTTP client | Planned |
| **3. Discord Bot** | Thin Discord client calling API over HTTP, slash commands, embed formatting | Planned |
| **4. User Auth + Web Frontend** | Discord OAuth, cookie auth, security hardening, Vite+React SPA | Planned |
| **5. PWA + Share Intent** | Service worker, Web Share Target API, push notifications | Future |

---

## 12. Resolved Decisions

Decisions made during architecture review:

1. **SQLite vs Postgres**: SQLite for now. EF Core abstraction makes migration straightforward when needed.
2. **API key rotation**: Static pre-shared key via Aspire parameters is sufficient for single-deployment. Rotation mechanism deferred.
3. **Player mapping UX**: Deferred to Phase 3 design. Will explore name-similarity suggestions alongside manual tagging.
4. **Multi-play file handling**: Single summary embed with a link to detailed web view (not one embed per play).
5. **Rate limiting scope**: Auth endpoints AND upload endpoints.
6. **Data Protection key storage**: Deferred — decide during Phase 4 (user auth).
7. **Wolverine.Fx**: Adopted. Start with HTTP endpoints only (`WolverineFx.Http`), add async messaging transport later.
8. **CSRF**: ASP.NET Core built-in anti-forgery tokens (not `X-Requested-With`).
9. **Dockerfiles**: Keep standalone Dockerfiles alongside Aspire for CI/CD portability.
10. **Mappers**: Switch to Mapperly source-generated mappers (replace manual static methods).
11. **ID types**: Vogen in Core/Data, plain Guid in Contracts.

## 13. Open Questions

1. **Database migration strategy**: EF Core migrations vs auto-migration on startup. Needs CI/CD pipeline decision.
2. **Error response format**: Wolverine ProblemDetails vs custom error DTOs in Contracts.
3. **Web frontend stack details**: State management, data fetching, styling approach (deferred to Phase 4).
