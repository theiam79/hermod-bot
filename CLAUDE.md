# Hermod Bot - Project Instructions

## Project Overview

Hermod is a play-sharing platform for board games recorded in the BGStats app. Originally a single-server Discord bot, it is being rewritten as an API-first platform with Discord as one client and a web frontend.

- **Repo**: `/mnt/data/repos/hermod-bot`
- **Architecture doc**: `docs/architecture.md`

## Stack

- **.NET 10** (SDK 10.0.103)
- **Aspire** for local dev orchestration (AppHost pattern)
  - Aspire CLI daily builds installed at `~/.aspire/bin/aspire`
- **Wolverine.Fx** for command/handler pattern and HTTP endpoint routing
  - `WolverineFx.Http` replaces raw Minimal API `MapGet`/`MapPost` for endpoint routing
  - `AutoApplyTransactions()` + `UseEntityFrameworkCoreTransactions()` for unit-of-work
  - Start with HTTP only; async messaging transport added later
- **EF Core + SQLite** for data access
- **Discord.Net 3.x + Discord.Addons.Hosting 5.x** — bot hosted services colocated inside Hermod.Api
- **Vite + React + TypeScript** for the web frontend (planned)
- **TUnit** for testing (NOT xUnit/NUnit/MSTest)

## Key Libraries

| Library | Purpose | Notes |
|---------|---------|-------|
| **Vogen** (8.x) | Strongly-typed value object IDs | `PrivateAssets="all"` in csproj. Earlier versions (5.x) have CS1040 errors on .NET 10 |
| **WolverineFx.Http** (5.x) | HTTP endpoint routing + command/handler pattern | Replaces raw Minimal API routing |
| **Mapperly** (4.x) | Source-generated object mapping | `PrivateAssets="all"` in csproj |
| **NCalcSync** (5.3.0) | Score expression evaluation | Note: version 5.2.12 does not exist on NuGet |
| **Discord.Net** (3.x) | Discord bot framework | — |
| **Discord.Addons.Hosting** (5.x) | DI/lifecycle wiring for Discord.Net | `DiscordClientService` base class; `ConfigureDiscordHost` / `UseInteractionService` |
| **TUnit** | Testing framework | `OutputType=Exe`, no `Microsoft.NET.Test.Sdk` needed |
| **AspNet.Security.OAuth.Discord** | Discord OAuth provider | For user authentication (Phase 4) |

## What We Do NOT Use

- No MediatR (Wolverine replaces this pattern)
- No AutoMapper (use Mapperly)
- No FluentValidation
- No FluentResults
- No xUnit/NUnit/MSTest (use TUnit)
- No separate Hermod.Bot project (bot is colocated in Hermod.Api)

## Solution Structure

```
Hermod.slnx
├── src/
│   ├── Hermod.AppHost/          # Aspire orchestrator
│   ├── Hermod.ServiceDefaults/  # Shared Aspire config
│   ├── Hermod.Api/              # ASP.NET API — data gateway + Discord bot (colocated)
│   │   ├── Discord/             # Discord hosted services
│   │   │   ├── BotService.cs        # Connects bot, bridges Client.Log
│   │   │   ├── GuildHandler.cs      # Upserts GroupEntity on guild join/ready
│   │   │   ├── InteractionHandler.cs # Registers and dispatches slash commands
│   │   │   ├── MessageReceivedHandler.cs # Handles .bgsplay file uploads
│   │   │   └── Modules/             # Slash command modules (InfoModule, InfoModule.Admin)
│   │   ├── Endpoints/           # Wolverine HTTP endpoint handlers
│   │   ├── Handlers/            # Wolverine message handlers
│   │   └── Messages/            # Wolverine message/event records
│   ├── Hermod.Data/             # EF Core entities, DbContext, config, migrations
│   └── Hermod.BGStats/          # .bgsplay file parsing (standalone, no DI)
├── tests/
│   └── Hermod.BGStats.Tests/    # TUnit tests for parsing
├── legacy/                      # Old .NET 6 code (reference only, do not modify)
└── sample-play-files/           # Test data (gitignored)
```

## Architecture Rules

- **Bot is colocated in Hermod.Api** — Discord hosted services run in the same process as the API
- **API is the single data gateway** — only Hermod.Api references Hermod.Data
- **User → API auth**: HttpOnly cookie sessions via Discord OAuth (BFF pattern, Phase 4)
- **BGStats** is a standalone parsing library with no framework dependencies

## Testing

- Framework: **TUnit** (source-generated, async-first)
- Test projects use `OutputType=Exe` — no `Microsoft.NET.Test.Sdk`
- Run tests: `dotnet test` or `dotnet run` on the test project
- Sample .bgsplay files in `sample-play-files/` (gitignored, not committed)
- Test projects copy sample files to output via csproj `<None Include>` with `CopyToOutputDirectory`

## Build & Run

```bash
# Build entire solution
dotnet build Hermod.slnx -verbosity:quiet

# Run with Aspire (requires Discord:Token in user secrets — see Known Gotchas)
~/.aspire/bin/aspire run --project src/Hermod.AppHost/Hermod.AppHost.csproj

# Run tests
dotnet test Hermod.slnx
```

**Build note**: Do NOT use `--no-incremental`. It can leave corrupted nested `bin/` directories.
If a build leaves unexpected state, delete `bin/` and `obj/` manually and rebuild.

## Known Gotchas

- **Vogen 5.x on .NET 10**: Generates preprocessor directives that cause CS1040 errors. Use Vogen 8.x+.
- **NCalcSync 5.2.12**: Does not exist on NuGet. Use 5.3.0.
- **Sandbox restrictions**: `dotnet new` may fail if HOME is not writable.
- **Aspire CLI**: Logs a warning about read-only filesystem for log files — harmless.
- **SQLite single-writer**: Data access is centralised in Hermod.Api to avoid write contention.
- **Windows-artifact bin\Debug directories**: On Linux, a folder literally named `bin\Debug` (backslash) can appear from Windows-generated build output. Not caught by `[Bb]in/` gitignore; covered by `*\\*`. Delete them if they appear.
- **Discord privileged intents**: `GatewayIntents.GuildMembers` and `GatewayIntents.MessageContent` must be enabled in the Discord Developer Portal under Bot → Privileged Gateway Intents. Without `MessageContent`, `message.Attachments` is always empty.
- **`global::Discord.Interactions.IResult`**: Within the `Hermod.Api.Discord` namespace, `IResult` is ambiguous with `Microsoft.AspNetCore.Http.IResult`. Use the fully-qualified form `global::Discord.Interactions.IResult`.
- **Discord token config**: The Aspire AppHost passes `Discord:Token` as the environment variable `Discord__Token` to Hermod.Api. Set it via user secrets on the AppHost project:
  ```bash
  dotnet user-secrets set "Parameters:discord-token" "<your-token>" --project src/Hermod.AppHost
  ```

## Conventions

- Use `record` types for DTOs and immutable models
- Use Vogen value objects for all entity IDs in Hermod.Data; plain `Guid` in any future Contracts layer
- Use Mapperly for entity ↔ DTO mapping (source-generated, not reflection)
- Wolverine endpoint organization: one static class per resource in `Endpoints/` using `[WolverineGet]`/`[WolverinePost]` etc.
- EF Core fluent configuration in `Configurations/` directory (one file per entity)
- EF Core migrations for schema changes (not `EnsureCreatedAsync`)
- **Wolverine message cascade**: handlers return the next message type (or `T?` for conditional dispatch — returning `null` skips the cascade)
- **Discord modules**: use `partial class` to split a slash command group across files (e.g. `InfoModule.cs` + `InfoModule.Admin.cs`) to avoid Discord.Net group registration conflicts
- **Singleton Discord services needing scoped services**: inject `IServiceScopeFactory`, create a scope per operation with `await using var scope = _scopeFactory.CreateAsyncScope()`
- **Wolverine handlers are static**: all dependencies injected as method parameters; no instance state
