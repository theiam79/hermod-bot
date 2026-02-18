# Task 02 — Register Discord Client in DI

## Why
`Discord.Addons.Hosting` provides extension methods that wire `DiscordSocketClient` and
`InteractionService` into .NET's hosted service lifecycle. This registration must happen in
`Program.cs` before any bot services are added.

## Steps

### Update `src/Hermod.Api/Program.cs`

Add the following after `builder.Services.AddDbContextWithWolverineIntegration<HermodContext>(...)`:

```csharp
builder.Services
    .AddDiscordHost((config, _) =>
    {
        config.Token = builder.Configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord:Token is not configured.");
        config.TokenType = TokenType.Bot;
    }, socketConfig =>
    {
        socketConfig.AlwaysDownloadUsers = true;
        socketConfig.MessageCacheSize = 200;
        socketConfig.GatewayIntents =
            GatewayIntents.Guilds |
            GatewayIntents.GuildMembers |   // privileged — enable in Discord dev portal
            GatewayIntents.GuildMessages;
    })
    .UseInteractionService((config, _) =>
    {
        config.LogLevel = LogSeverity.Info;
        config.UseCompiledLambda = true;
    });
```

Add required usings:
```csharp
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
```

## Notes
- `AddDiscordHost` registers `DiscordSocketClient` as a singleton and manages its lifetime
  via `IHostedService` internally.
- `UseInteractionService` registers `InteractionService` as a singleton scoped to the client.
- `AlwaysDownloadUsers = true` — ensures guild member lists are populated in the cache.
  Required for later member-lookup functionality (checking if an uploader is in a guild).
- `GatewayIntents.GuildMembers` is a **privileged intent** — must be enabled in the Discord
  Developer Portal (Application → Bot → Privileged Gateway Intents) or the bot will fail
  to connect with a `4014` close code.
- `GatewayIntents.GuildMessages` is needed for the future message-received handler that
  processes `.bgsplay` attachments sent via the BGStats share intent.
- `UseCompiledLambda = true` on `InteractionService` improves runtime performance of
  slash command dispatch.

## Acceptance Criteria
- [ ] `Program.cs` calls `AddDiscordHost` and `UseInteractionService`
- [ ] `Discord:Token` is read from configuration with a clear error if missing
- [ ] `GatewayIntents` includes `Guilds`, `GuildMembers`, `GuildMessages`
- [ ] `dotnet build src/Hermod.Api/Hermod.Api.csproj` succeeds
