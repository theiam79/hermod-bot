# Hermod Bot - Project Instructions

## Project Overview

Hermod is a play-sharing platform for board games recorded in the BGStats app. Originally a single-server Discord bot, it is being rewritten as an API-first platform with Discord as one client and a web frontend.

## Key Libraries

| Library | Purpose | Notes |
|---------|---------|-------|
| **Vogen** (8.x) | Strongly-typed value object IDs | `PrivateAssets="all"` in csproj. Earlier versions (5.x) have CS1040 errors on .NET 10 |
| **WolverineFx.Http** (5.x) | HTTP endpoint routing + command/handler pattern | Replaces raw Minimal API routing |
| **WolverineFx.Nats** (5.x) | NATS transport for cross-process messaging | Bot ↔ API communication via Core NATS subjects |
| **WolverineFx.Postgresql** (5.x) | PostgreSQL message persistence (API only) | Provides `DatabaseSettings` for EF Core integration |
| **Mapperly** (4.x) | Source-generated object mapping | `PrivateAssets="all"` in csproj |
| **NCalcSync** (5.3.0) | Score expression evaluation | Note: version 5.2.12 does not exist on NuGet |
| **Discord.Net** (3.x) | Discord bot framework | Only in Hermod.Bot |
| **Discord.Addons.Hosting** (6.x) | DI/lifecycle wiring for Discord.Net | `DiscordClientService` base class |
| **Aspire.Npgsql.EntityFrameworkCore.PostgreSQL** | Aspire EF Core client integration | Prefer over raw `Npgsql.EntityFrameworkCore.PostgreSQL` in service projects |
| **TUnit** | Testing framework | `OutputType=Exe`, no `Microsoft.NET.Test.Sdk` needed |
| **AspNet.Security.OAuth.Discord** | Discord OAuth provider | For user authentication (Phase 4) |

## Architecture Rules

- **End clients do not access the core database** — Discord modules/services (and any future clients) must never reference `HermodContext` or `Hermod.Data`. All data access goes through Wolverine commands/queries via `IMessageBus`.
- **Core data model is platform-agnostic** — no Discord/platform-specific fields in `Hermod.Data` entities. Client-specific data lives in the client project (e.g. `BotDbContext` in `Hermod.Bot`).
- **Bot runs in Hermod.Bot** — separate process from the API, communicating via Wolverine NATS transport
- **API is the single data gateway** — only Hermod.Api references Hermod.Data
- **User → API auth**: HttpOnly cookie sessions via Discord OAuth (BFF pattern, Phase 4)
- **BGStats** is a standalone parsing library with no framework dependencies

## Testing

- Framework: **TUnit** (source-generated, async-first)
- Test projects use `OutputType=Exe` — no `Microsoft.NET.Test.Sdk`
- Run tests: `dotnet test --solution Hermod.slnx` or `dotnet run` on the test project
- Sample .bgsplay files in `sample-play-files/` (gitignored, not committed)
- Test projects copy sample files to output via csproj `<None Include>` with `CopyToOutputDirectory`

### TUnit lifecycle: use `[ClassDataSource]` property injection chains

All shared test infrastructure (containers, fixtures, factories) **must** use TUnit's `[ClassDataSource<T>(Shared = SharedType.PerTestSession)]` property injection — never manual `[Before(Class)]`/`[After(Class)]` hooks or static fields. This gives TUnit control over initialization order and, critically, **reverse-order disposal** (fixtures dispose before their dependencies).

- Implement `IAsyncInitializer` (from `TUnit.Core.Interfaces`) for setup
- Implement `IAsyncDisposable` for teardown
- Express dependencies as `[ClassDataSource<T>]` properties — TUnit resolves the graph automatically
- Tests consume the top-level fixture via `[ClassDataSource<ApiFixture>]` property injection

## Build & Run

```bash
# Build entire solution
dotnet build Hermod.slnx -verbosity:quiet

# Run with Aspire (requires Discord:Token in user secrets — see Known Gotchas)
~/.aspire/bin/aspire run --project src/Hermod.AppHost/Hermod.AppHost.csproj

# Run tests
dotnet test --solution Hermod.slnx
```

**Build note**: Do NOT use `--no-incremental`. It can leave corrupted nested `bin/` directories.
If a build leaves unexpected state, delete `bin/` and `obj/` manually and rebuild.

## Known Gotchas

- **Vogen 5.x on .NET 10**: Generates preprocessor directives that cause CS1040 errors. Use Vogen 8.x+.
- **NCalcSync 5.2.12**: Does not exist on NuGet. Use 5.3.0.
- **Sandbox restrictions**: `dotnet new` may fail if HOME is not writable.
- **Aspire CLI**: Logs a warning about read-only filesystem for log files — harmless.
- **PostgreSQL via Aspire**: The AppHost provisions a PostgreSQL container with a `hermod-db` database. The connection string is auto-injected as `ConnectionStrings:hermod-db`. For `dotnet ef` CLI outside Aspire, set `HERMOD_CONNECTION_STRING` env var or use the default `Host=localhost;Database=hermod;Username=postgres;Password=postgres`.
- **Windows-artifact bin\Debug directories**: On Linux, a folder literally named `bin\Debug` (backslash) can appear from Windows-generated build output. Not caught by `[Bb]in/` gitignore; covered by `*\\*`. Delete them if they appear.
- **Discord privileged intents**: `GatewayIntents.GuildMembers` and `GatewayIntents.MessageContent` must be enabled in the Discord Developer Portal under Bot → Privileged Gateway Intents. Without `MessageContent`, `message.Attachments` is always empty.
- **`global::Discord.Interactions.IResult`**: Within the `Hermod.Bot` namespace, `IResult` may be ambiguous. Use the fully-qualified form `global::Discord.Interactions.IResult`.
- **`Discord.IMessage` vs `Wolverine.IMessage`**: In modules that inject `IMessageBus`, use `Discord.IMessage` explicitly in Discord message command parameters.
- **Discord token config**: The Aspire AppHost passes `Discord:Token` as the environment variable `Discord__Token` to Hermod.Bot. Set it via user secrets on the AppHost project:
  ```bash
  dotnet user-secrets set "Parameters:discord-token" "<your-token>" --project src/Hermod.AppHost
  ```

## Conventions

- Use `record` types for DTOs and immutable models
- Use Vogen value objects for all entity IDs in Hermod.Data; plain `Guid` in Hermod.Messages (no dependency on Hermod.Data)
- Use Mapperly for entity ↔ DTO mapping (source-generated, not reflection)
- Wolverine endpoint organization: one static class per resource in `Endpoints/` using `[WolverineGet]`/`[WolverinePost]` etc.
- EF Core fluent configuration in `Configurations/` directory (one file per entity)
- EF Core migrations for schema changes (not `EnsureCreatedAsync`)
- **Wolverine message cascade**: handlers return the next message type (or `T?` for conditional dispatch — returning `null` skips the cascade)
- **Wolverine NATS transport**: Api listens on `hermod.api`, Bot listens on `hermod.bot`. Commands/events are routed via `PublishMessage<T>().ToNatsSubject()`. Core NATS (at-most-once, in-memory) — no JetStream.
- **Discord modules are thin** — validate input, dispatch Wolverine command/query via `IMessageBus`, respond to user
- **Discord modules**: use `partial class` to split a slash command group across files (e.g. `InfoModule.cs` + `InfoModule.Admin.cs`) to avoid Discord.Net group registration conflicts
- **Singleton Discord services needing scoped services**: inject `IServiceScopeFactory`, create a scope per operation with `await using var scope = _scopeFactory.CreateAsyncScope()`
- **Wolverine handlers are static**: all dependencies injected as method parameters; no instance state
- **Prefer Aspire client integration packages** (`Aspire.*`) over raw provider packages in service projects. They add health checks, OpenTelemetry tracing/metrics, and resilience automatically. Use `Add*()` when Aspire can own registration; use `Enrich*()` when a third-party library (e.g. Wolverine) must register the service first. Disable Aspire retries (`DisableRetry = true`) on DbContexts managed by Wolverine, which handles retries at the handler level.
