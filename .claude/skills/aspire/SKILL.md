---
name: aspire
description: Use the Aspire CLI to create, run, manage, and publish Aspire-based .NET applications. Invoke this skill when the user asks to scaffold projects, run the AppHost, manage resources, view logs/telemetry, add integrations, publish, deploy, or interact with the Aspire CLI in any way.
argument-hint: "[command] [args...]"
allowed-tools: Bash
---

# Aspire CLI Skill

You are an expert at using the .NET Aspire CLI. This project uses **daily builds** of the Aspire CLI (v13.3.0-preview+). Always pass `--nologo` to suppress the startup banner. When automating, pass `--non-interactive` to disable prompts and spinners.

The Aspire CLI binary is at: `~/.aspire/bin/aspire`

## User Request

$ARGUMENTS

## Global Options (available on all commands)

| Flag | Description |
|------|-------------|
| `--nologo` | Suppress startup banner and telemetry notice |
| `--non-interactive` | Disable interactive prompts/spinners (CI/automation) |
| `-v, --debug-level <level>` | Console log level: Trace, Debug, Information, Warning, Error, Critical |
| `-?, -h, --help` | Show help |
| `--version` | Show version (top-level only) |

## Command Reference

### `aspire new` - Create a new Aspire project

Creates a new project from a template. In interactive mode, presents a template picker.

```bash
aspire new [template] --nologo [options]
```

**Templates:**
| Template ID | Description |
|-------------|-------------|
| `aspire-starter` | Starter App (ASP.NET Core / Blazor) |
| `aspire-ts-cs-starter` | Starter App (ASP.NET Core / React+TypeScript) |
| `aspire-py-starter` | Starter App (FastAPI / React) |
| `aspire-apphost-singlefile` | Empty AppHost (single file) |

**Options:**
| Flag | Description |
|------|-------------|
| `-n, --name <name>` | Project name |
| `-o, --output <path>` | Output directory |
| `-s, --source <source>` | NuGet source for templates |
| `-v, --version <version>` | Template package version |
| `--channel <channel>` | Template channel: `stable`, `daily` |

**Examples:**
```bash
# Create a React+ASP.NET starter in current directory
aspire new aspire-ts-cs-starter -n MyApp --nologo

# Create with daily channel templates
aspire new aspire-starter -n MyApp --channel daily --nologo

# Create an empty AppHost
aspire new aspire-apphost-singlefile -n MyApp --nologo
```

---

### `aspire init` - Add Aspire to existing solution

Analyzes the current solution and adds Aspire AppHost + ServiceDefaults projects. Can also create a single-file AppHost.

```bash
aspire init --nologo [options]
```

**Options:**
| Flag | Description |
|------|-------------|
| `-s, --source <source>` | NuGet source |
| `-v, --version <version>` | Template version |
| `--channel <channel>` | `stable` or `daily` |

**Examples:**
```bash
# Initialize Aspire in existing solution (interactive - picks projects to enlist)
aspire init --nologo

# Non-interactive init
aspire init --nologo --non-interactive
```

---

### `aspire run` - Run AppHost in dev mode

Discovers the AppHost project, builds, verifies certs, starts the dashboard, and runs all resources.

```bash
aspire run --nologo [options] [-- <additional-args>]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--project <path>` | Path to AppHost .csproj (auto-detected if omitted) |
| `--detach` | Run in background, exit after start |
| `--format <Json\|Table>` | Output format (only with `--detach`) |
| `--isolated` | Randomized ports, allows multiple instances |
| `--no-build` | Skip build/restore |

**Examples:**
```bash
# Run the AppHost (auto-discovers project)
aspire run --nologo

# Run a specific AppHost project
aspire run --project src/Hermod.AppHost/Hermod.AppHost.csproj --nologo

# Run in background
aspire run --detach --nologo

# Run without building (already built)
aspire run --no-build --nologo

# Pass args through to the AppHost
aspire run --nologo -- --urls=http://localhost:5100
```

---

### `aspire ps` - List running AppHosts

```bash
aspire ps --nologo [--format Json|Table]
```

---

### `aspire resources [resource]` - View resource status

```bash
aspire resources --nologo [options]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--project <path>` | AppHost project path |
| `--watch` | Stream changes (NDJSON with `--format Json`) |
| `--format <Json\|Table>` | Output format |

**Examples:**
```bash
# Show all resources
aspire resources --nologo

# Watch resource changes live
aspire resources --watch --nologo

# Show a specific resource
aspire resources api --nologo
```

---

### `aspire logs [resource]` - View resource logs

```bash
aspire logs [resource] --nologo [options]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--project <path>` | AppHost project path |
| `-f, --follow` | Stream logs in real-time |
| `--format <Json\|Table>` | Output format |
| `-n, --tail <n>` | Last N lines (default: all) |

**Examples:**
```bash
# Tail all logs
aspire logs -f --nologo

# Last 50 lines from api resource
aspire logs api -n 50 --nologo

# Follow a specific resource
aspire logs bot -f --nologo
```

---

### `aspire stop [resource]` - Stop AppHost or resource

```bash
# Stop entire AppHost
aspire stop --nologo

# Stop a specific resource
aspire stop api --nologo --project src/Hermod.AppHost/Hermod.AppHost.csproj
```

### `aspire start <resource>` - Start a stopped resource

```bash
aspire start api --nologo
```

### `aspire restart <resource>` - Restart a resource

```bash
aspire restart api --nologo
```

### `aspire wait <resource>` - Wait for resource readiness

```bash
aspire wait api --nologo [options]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--status <status>` | Target: `healthy` (default), `up`, `down` |
| `--timeout <seconds>` | Max wait time (default: 120) |

**Examples:**
```bash
# Wait for API to be healthy
aspire wait api --nologo

# Wait for resource to come up, 60s timeout
aspire wait db --status up --timeout 60 --nologo
```

---

### `aspire add [integration]` - Add hosting integration

Adds an Aspire hosting integration NuGet package to the AppHost.

```bash
aspire add [integration] --nologo [options]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--project <path>` | Target project for the integration |
| `-v, --version <version>` | Package version |
| `-s, --source <source>` | NuGet source |

**Examples:**
```bash
# Interactive integration picker
aspire add --nologo

# Add Redis integration
aspire add redis --nologo

# Add to specific project
aspire add postgres --project src/Hermod.AppHost/Hermod.AppHost.csproj --nologo
```

---

### `aspire publish` - Generate deployment artifacts (Preview)

Generates Bicep, Docker Compose, Kubernetes Helm charts, etc.

```bash
aspire publish --nologo [options] [-- <additional-args>]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--project <path>` | AppHost project |
| `-o, --output-path <path>` | Output dir (default: `./aspire-output`) |
| `--log-level <level>` | Pipeline log level |
| `-e, --environment <env>` | Environment (default: Production) |
| `--no-build` | Skip build |
| `--include-exception-details` | Show stack traces |

---

### `aspire deploy` - Deploy to targets (Preview)

```bash
aspire deploy --nologo [options] [-- <additional-args>]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--project <path>` | AppHost project |
| `-o, --output-path <path>` | Artifact output path |
| `-e, --environment <env>` | Environment (default: Production) |
| `--no-build` | Skip build |
| `--clear-cache` | Clear deployment cache |

---

### `aspire do <step>` - Execute pipeline step (Preview)

```bash
aspire do <step> --nologo [options] [-- <additional-args>]
```

---

### `aspire update` - Update integrations (Preview)

```bash
aspire update --nologo [options]
```

**Options:**
| Flag | Description |
|------|-------------|
| `--project <path>` | AppHost project |
| `--self` | Update the Aspire CLI itself |
| `--channel <channel>` | `stable` or `daily` |

**Examples:**
```bash
# Update all Aspire packages in AppHost
aspire update --nologo

# Update the CLI itself to latest daily
aspire update --self --channel daily --nologo
```

---

### `aspire command <resource> <command>` - Execute resource command

```bash
aspire command mydb migrate --nologo
```

---

### `aspire config` - Manage CLI configuration

```bash
aspire config list --nologo          # List all settings
aspire config get <key> --nologo     # Get a setting
aspire config set <key> <value> --nologo  # Set a setting
aspire config delete <key> --nologo  # Delete a setting
```

---

### `aspire cache` - Manage CLI cache

```bash
aspire cache clear --nologo    # Clear all cached data
```

---

### `aspire doctor` - Diagnose environment

```bash
aspire doctor --nologo [--format Json|Table]
```

---

### `aspire telemetry` - View OpenTelemetry data

**Structured logs:**
```bash
aspire telemetry logs [resource] --nologo [options]
```
| Flag | Description |
|------|-------------|
| `-f, --follow` | Stream in real-time |
| `--format <Json\|Table>` | Output format |
| `-n, --limit <n>` | Max items |
| `--trace-id <id>` | Filter by trace |
| `--severity <level>` | Min severity filter |

**Spans:**
```bash
aspire telemetry spans [resource] --nologo [options]
```
| Flag | Description |
|------|-------------|
| `-f, --follow` | Stream in real-time |
| `--format <Json\|Table>` | Output format |
| `-n, --limit <n>` | Max items |
| `--trace-id <id>` | Filter by trace |
| `--has-error` | Show only errors |

**Traces:**
```bash
aspire telemetry traces [resource] --nologo [options]
```
| Flag | Description |
|------|-------------|
| `--format <Json\|Table>` | Output format |
| `-n, --limit <n>` | Max items |
| `-t, --trace-id <id>` | Filter by trace |
| `--has-error` | Show only errors |

---

### `aspire agent` - AI agent integrations

```bash
aspire agent mcp --nologo     # Start MCP server (for AI tool integration)
aspire agent init --nologo    # Initialize agent environment config
```

---

### `aspire docs` - Browse documentation

```bash
aspire docs list --nologo              # List all doc pages
aspire docs search <query> --nologo    # Search docs
aspire docs get <slug> --nologo        # Get full page content
```

---

## Best Practices

1. **Always use `--nologo`** to keep output clean
2. **Use `--non-interactive`** when running in scripts or automation
3. **Use `--project`** to be explicit about which AppHost to target when multiple exist
4. **Use `--detach`** for background runs, then monitor with `aspire resources --watch` or `aspire logs -f`
5. **Use `aspire doctor`** to diagnose environment issues before troubleshooting
6. **Use `aspire docs get <slug>`** to look up integration-specific documentation
7. This project uses **daily builds** - when creating templates or updating, use `--channel daily`
8. Use `aspire resources --format Json` or `aspire ps --format Json` when you need to parse output programmatically
