---
name: dotnet
description: Use the .NET CLI (dotnet) to build, test, run, publish, and manage .NET projects and solutions. Invoke this skill when the user asks to create projects, add packages/references, manage solutions, run builds or tests, or any dotnet CLI operation.
argument-hint: "[command] [args...]"
allowed-tools: Bash
---

# .NET CLI Skill

You are an expert at using the .NET CLI. This project targets **.NET 10** (SDK 10.0.103 installed). Always prefer the modern command syntax where available.

## User Request

$ARGUMENTS

## Command Reference

### `dotnet build` - Build a project or solution

```bash
dotnet build [<PROJECT|SOLUTION>] [options]
```

| Flag | Description |
|------|-------------|
| `-c, --configuration <cfg>` | Build configuration (`Debug`, `Release`) |
| `-f, --framework <tfm>` | Target framework |
| `-o, --output <dir>` | Output directory |
| `--no-restore` | Skip restore |
| `-v, --verbosity <level>` | `q[uiet]`, `m[inimal]`, `n[ormal]`, `d[etailed]`, `diag[nostic]` |
| `--artifacts-path <dir>` | Centralized artifacts output |

**Examples:**
```bash
# Build entire solution
dotnet build

# Build specific project in Release
dotnet build src/Hermod.Api/Hermod.Api.csproj -c Release

# Build with minimal output
dotnet build -v m
```

---

### `dotnet test` - Run tests

```bash
dotnet test [<PROJECT|SOLUTION>] [options] [-- <additional-args>]
```

| Flag | Description |
|------|-------------|
| `-c, --configuration <cfg>` | Configuration |
| `--filter <expr>` | Test filter expression |
| `-t, --list-tests` | List tests without running |
| `--no-build` | Skip build |
| `--no-restore` | Skip restore |
| `-v, --verbosity <level>` | Verbosity |
| `-l, --logger <logger>` | Test logger (e.g., `trx`, `console`) |
| `--results-directory <dir>` | Results output directory |
| `-e, --environment <NAME=VALUE>` | Set environment variable |

**Examples:**
```bash
# Run all tests in solution
dotnet test

# Run specific test project
dotnet test tests/Hermod.BGStats.Tests/Hermod.BGStats.Tests.csproj

# Run with filter
dotnet test --filter "FullyQualifiedName~ParseTests"

# List tests without running
dotnet test -t

# Run without rebuilding
dotnet test --no-build
```

**Note:** This project uses **TUnit** which builds on Microsoft.Testing.Platform. Tests can also be run directly via `dotnet run` on the test project (see TUnit skill for details).

---

### `dotnet run` - Build and run a project

```bash
dotnet run [options] [-- <app-args>]
```

| Flag | Description |
|------|-------------|
| `--project <path>` | Project to run |
| `-c, --configuration <cfg>` | Configuration |
| `-f, --framework <tfm>` | Target framework |
| `--no-build` | Skip build |
| `-lp, --launch-profile <name>` | Launch profile from launchSettings.json |
| `--no-launch-profile` | Ignore launch profiles |

**Examples:**
```bash
# Run current project
dotnet run

# Run specific project
dotnet run --project src/Hermod.Api/Hermod.Api.csproj

# Run with args passed to the app
dotnet run --project src/Hermod.Api -- --urls=http://localhost:5000

# Run without rebuilding
dotnet run --no-build
```

---

### `dotnet new` - Create new projects/files

```bash
dotnet new <TEMPLATE> [options]
```

| Flag | Description |
|------|-------------|
| `-n, --name <name>` | Project/file name |
| `-o, --output <dir>` | Output directory |
| `-f, --framework <tfm>` | Target framework |
| `--force` | Overwrite existing files |
| `--dry-run` | Show what would be created |

**Common Templates:**
| Template | Description |
|----------|-------------|
| `classlib` | Class library |
| `console` | Console app |
| `web` | Empty ASP.NET Core web app |
| `webapi` | ASP.NET Core Web API |
| `worker` | Worker Service |
| `sln` | Solution file |
| `gitignore` | .gitignore file |
| `globaljson` | global.json file |
| `nugetconfig` | NuGet.Config file |
| `editorconfig` | .editorconfig file |

**Examples:**
```bash
# Create class library
dotnet new classlib -n Hermod.Core -o src/Hermod.Core

# Create worker service
dotnet new worker -n Hermod.Bot -o src/Hermod.Bot

# Create web API
dotnet new webapi -n Hermod.Api -o src/Hermod.Api

# Create solution file
dotnet new sln -n Hermod

# Create global.json pinning SDK version
dotnet new globaljson --sdk-version 10.0.103
```

---

### `dotnet solution` - Manage solution files

```bash
dotnet solution [<SLN_FILE>] <command> [options]
```

**Commands:**
```bash
# Add project to solution
dotnet solution add src/Hermod.Core/Hermod.Core.csproj

# Add to solution folder
dotnet solution add src/Hermod.Core/Hermod.Core.csproj --solution-folder src

# List projects in solution
dotnet solution list

# Remove project from solution
dotnet solution remove src/OldProject/OldProject.csproj

# Migrate .sln to .slnx format
dotnet solution migrate
```

---

### `dotnet package` - Manage NuGet packages

```bash
dotnet package <command> [options]
```

**Commands:**
```bash
# Add package to project
dotnet package add Vogen --project src/Hermod.Core/Hermod.Core.csproj

# Add specific version
dotnet package add Discord.Net --version 3.16.0 --project src/Hermod.Bot/Hermod.Bot.csproj

# Add prerelease
dotnet package add TUnit --prerelease --project tests/Hermod.BGStats.Tests/Hermod.BGStats.Tests.csproj

# List packages in project
dotnet package list --project src/Hermod.Core/Hermod.Core.csproj

# List outdated packages
dotnet package list --outdated

# Remove package
dotnet package remove AutoMapper --project src/Hermod.Core/Hermod.Core.csproj

# Search for packages
dotnet package search Mapperly

# Update packages
dotnet package update --project src/Hermod.Core/Hermod.Core.csproj
```

---

### `dotnet reference` - Manage project references

```bash
dotnet reference [--project <project>] <command> [options]
```

**Commands:**
```bash
# Add project reference
dotnet reference src/Hermod.Core/Hermod.Core.csproj add src/Hermod.Data/Hermod.Data.csproj

# List references
dotnet reference src/Hermod.Core/Hermod.Core.csproj list

# Remove reference
dotnet reference src/Hermod.Core/Hermod.Core.csproj remove src/Hermod.Data/Hermod.Data.csproj
```

---

### `dotnet restore` - Restore dependencies

```bash
dotnet restore [<PROJECT|SOLUTION>] [options]
```

| Flag | Description |
|------|-------------|
| `--source <source>` | NuGet source URL |
| `--packages <dir>` | Packages directory |
| `--no-cache` | Don't cache packages |
| `--force` | Force re-resolve |

---

### `dotnet publish` - Publish for deployment

```bash
dotnet publish [<PROJECT>] [options]
```

| Flag | Description |
|------|-------------|
| `-c, --configuration <cfg>` | Configuration (default: Release for publish) |
| `-o, --output <dir>` | Output directory |
| `-r, --runtime <rid>` | Target runtime (e.g., `linux-x64`) |
| `--self-contained` | Include .NET runtime |
| `--no-self-contained` | Framework-dependent |
| `-p:PublishSingleFile=true` | Single-file publish |
| `-p:PublishTrimmed=true` | IL trimming |

---

### `dotnet clean` - Clean build outputs

```bash
dotnet clean [<PROJECT|SOLUTION>] [-c <cfg>] [-o <dir>]
```

---

### `dotnet format` - Apply code style

```bash
dotnet format [<PROJECT|SOLUTION>] [options]
```

| Flag | Description |
|------|-------------|
| `--verify-no-changes` | Check without modifying (CI mode) |
| `--severity <level>` | `info`, `warn`, `error` |
| `--diagnostics <ids>` | Specific analyzer IDs |
| `--include <glob>` | Files to include |
| `--exclude <glob>` | Files to exclude |

---

### `dotnet watch` - File watcher with hot reload

```bash
dotnet watch [command] [options]
```

**Examples:**
```bash
# Watch and run with hot reload
dotnet watch --project src/Hermod.Api/Hermod.Api.csproj

# Watch and run tests on changes
dotnet watch test --project tests/Hermod.BGStats.Tests/

# Watch with specific launch profile
dotnet watch --project src/Hermod.Api -- --urls=http://localhost:5000
```

---

### `dotnet tool` - Manage .NET tools

```bash
# Install global tool
dotnet tool install -g dotnet-ef

# Install local tool (from manifest)
dotnet tool restore

# List installed tools
dotnet tool list -g

# Update tool
dotnet tool update -g dotnet-ef
```

---

### `dotnet ef` - Entity Framework Core (if installed)

```bash
# Add migration
dotnet ef migrations add InitialCreate --project src/Hermod.Data --startup-project src/Hermod.Api

# Update database
dotnet ef database update --project src/Hermod.Data --startup-project src/Hermod.Api

# Generate SQL script
dotnet ef migrations script --project src/Hermod.Data --startup-project src/Hermod.Api

# Remove last migration
dotnet ef migrations remove --project src/Hermod.Data --startup-project src/Hermod.Api
```

---

### `dotnet user-secrets` - Manage dev secrets

```bash
# Initialize secrets for a project
dotnet user-secrets init --project src/Hermod.Api

# Set a secret
dotnet user-secrets set "DiscordToken" "your-token-here" --project src/Hermod.Api

# List secrets
dotnet user-secrets list --project src/Hermod.Api

# Remove a secret
dotnet user-secrets remove "DiscordToken" --project src/Hermod.Api
```

---

## Best Practices

1. **Use solution-level commands** (`dotnet build`, `dotnet test`) to operate on all projects at once
2. **Use `--no-restore`** on subsequent builds after the first restore to save time
3. **Use `-v m`** (minimal verbosity) for cleaner build output
4. **Use `--project`** to target a specific project when running from the repo root
5. **Prefer `dotnet package add`** over manually editing .csproj for NuGet packages
6. **Use `dotnet solution add --solution-folder`** to organize projects in the solution
