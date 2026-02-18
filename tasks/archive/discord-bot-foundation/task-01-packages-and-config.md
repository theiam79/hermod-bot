# Task 01 — Add Discord.Net Packages and Configuration

## Why
Discord.Net and Discord.Addons.Hosting need to be added to `Hermod.Api` before any bot code
can be written. Configuration for the bot token must be wired through user secrets (dev) and
Aspire parameters (all environments) so the token never appears in source.

## Steps

### 1. Add NuGet packages to `src/Hermod.Api/Hermod.Api.csproj`

```xml
<PackageReference Include="Discord.Net" Version="3.*" />
<PackageReference Include="Discord.Addons.Hosting" Version="5.*" />
```

### 2. Add bot token to user secrets (local dev)

```bash
dotnet user-secrets set "Discord:Token" "<your-bot-token>" --project src/Hermod.Api
```

This stores the token in the local user secrets store — it is never committed to source control.

### 3. Update `src/Hermod.AppHost/AppHost.cs`

Add the Discord token as an Aspire secret parameter and pass it to the API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api")
    .WithEnvironment("Discord__Token", discordToken);

builder.Build().Run();
```

Aspire will source `discord-token` from user secrets on the AppHost project during local dev.
Add the token value to the AppHost's user secrets:

```bash
dotnet user-secrets set "Parameters:discord-token" "<your-bot-token>" --project src/Hermod.AppHost
```

## Notes
- `Discord.Addons.Hosting` v5.x targets `net6.0+` and works on .NET 10.
- The double-underscore `Discord__Token` in `WithEnvironment` maps to `Discord:Token` in
  ASP.NET Core's configuration system (section separator convention).
- The bot token should be kept in the Discord Developer Portal under your application →
  Bot → Token. Regenerate it if it was ever exposed.
- The `GuildMembers` gateway intent used in Task 02 is a **privileged intent** — it must be
  explicitly enabled in the Discord Developer Portal under your application → Bot →
  Privileged Gateway Intents. Enable it now so it's ready when member lookups are needed.

## Acceptance Criteria
- [ ] `Hermod.Api.csproj` references `Discord.Net` and `Discord.Addons.Hosting`
- [ ] `dotnet build src/Hermod.Api/Hermod.Api.csproj` succeeds
- [ ] `Discord:Token` is present in user secrets for `Hermod.Api` (or `Hermod.AppHost`)
- [ ] `AppHost.cs` passes the token as `Discord__Token` environment variable
- [ ] Bot token does not appear in any committed file
