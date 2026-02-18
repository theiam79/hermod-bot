# Hermod Bot - Project Instructions

## Project Overview

Hermod is a play-sharing platform for board games recorded in the BGStats app. Originally a single-server Discord bot, it is being rewritten as an API-first platform with Discord as one client and a web frontend.

- **Repo**: `/mnt/data/repos/hermod-bot`
- **Architecture doc**: `docs/architecture.md`
- **Plan file**: See Claude Code plan mode

## Stack

- **.NET 10** (SDK 10.0.103)
- **Aspire** for local dev orchestration (AppHost pattern)
  - Aspire CLI daily builds installed at `~/.aspire/bin/aspire`
- **Wolverine.Fx** for command/handler pattern and HTTP endpoint routing
  - `WolverineFx.Http` replaces raw Minimal API `MapGet`/`MapPost` for endpoint routing
  - Start with HTTP only; async messaging transport added later
- **EF Core + SQLite** for data access
- **Discord.Net** for the bot (Worker Service)
- **Vite + React + TypeScript** for the web frontend (planned)
- **TUnit** for testing (NOT xUnit/NUnit/MSTest)

## Key Libraries

| Library | Purpose | Notes |
|---------|---------|-------|
| **Vogen** (8.x) | Strongly-typed value object IDs | `PrivateAssets="all"` in csproj. Earlier versions (5.x) have CS1040 errors on .NET 10 |
| **WolverineFx.Http** (5.x) | HTTP endpoint routing + command/handler pattern | Replaces raw Minimal API routing |
| **Mapperly** (4.x) | Source-generated object mapping | `PrivateAssets="all"` in csproj |
| **NCalcSync** (5.3.0) | Score expression evaluation | Note: version 5.2.12 does not exist on NuGet |
| **Discord.Net** | Discord bot framework | — |
| **TUnit** | Testing framework | `OutputType=Exe`, no `Microsoft.NET.Test.Sdk` needed |
| **AspNet.Security.OAuth.Discord** | Discord OAuth provider | For user authentication |

## What We Do NOT Use

- No MediatR (Wolverine replaces this pattern)
- No AutoMapper (use Mapperly)
- No FluentValidation
- No FluentResults
- No xUnit/NUnit/MSTest (use TUnit)

## Solution Structure

```
Hermod.slnx
├── src/
│   ├── Hermod.AppHost/          # Aspire orchestrator
│   ├── Hermod.ServiceDefaults/  # Shared Aspire config
│   ├── Hermod.Api/              # ASP.NET Minimal API (single data gateway)
│   ├── Hermod.Bot/              # Discord bot (thin client, calls API over HTTP)
│   ├── Hermod.Web/              # Vite + React SPA (planned)
│   ├── Hermod.Contracts/        # Shared DTOs + typed HttpClient (planned)
│   ├── Hermod.Core/             # Business logic (only referenced by API)
│   ├── Hermod.Data/             # EF Core entities, DbContext, config
│   └── Hermod.BGStats/          # .bgsplay file parsing (standalone)
├── tests/
│   └── Hermod.BGStats.Tests/    # TUnit tests for parsing
├── legacy/                      # Old .NET 6 code (reference only)
└── sample-play-files/           # Test data (gitignored)
```

## Architecture Rules

- **Bot calls API over HTTP** — it does NOT reference Core or Data directly
- **API is the single data gateway** — Core and Data only live in the API process
- **Bot → API auth**: Pre-shared API key via Aspire parameters
- **User → API auth**: HttpOnly cookie sessions via Discord OAuth (BFF pattern)
- **Contracts project** uses plain `Guid` for IDs (not Vogen) to avoid coupling clients
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
dotnet build Hermod.slnx

# Run with Aspire
~/.aspire/bin/aspire run --project src/Hermod.AppHost/Hermod.AppHost.csproj

# Run tests
dotnet test Hermod.slnx
```

## Known Gotchas

- **Vogen 5.x on .NET 10**: Generates preprocessor directives that cause CS1040 errors. Use Vogen 8.x+.
- **NCalcSync 5.2.12**: Does not exist on NuGet. Use 5.3.0.
- **Sandbox restrictions**: `dotnet new` may fail if HOME is not writable.
- **Aspire CLI**: Logs a warning about read-only filesystem for log files — harmless.
- **SQLite single-writer**: This is why the Bot goes through the API instead of accessing the DB directly.

## Conventions

- Use `record` types for DTOs and immutable models
- Use Vogen value objects for all entity IDs in the Data/Core layers; plain `Guid` in Contracts
- Use Mapperly for entity ↔ domain model ↔ DTO mapping (source-generated, not reflection)
- Wolverine endpoint organization: one static class per resource in `Endpoints/` directory using `[WolverineGet]`/`[WolverinePost]` etc.
- Services registered as scoped via `AddHermodCore()` extension method
- EF Core fluent configuration in `Configurations/` directory (one file per entity)
- EF Core migrations for schema changes (not `EnsureCreatedAsync`)
