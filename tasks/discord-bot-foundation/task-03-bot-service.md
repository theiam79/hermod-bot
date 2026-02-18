# Task 03 — BotService: Connection and Logging

## Why
`BotService` is the minimal hosted service that bridges Discord.Net's internal logging to
.NET's `ILogger` and confirms the bot is connected on startup. All other bot services
depend on the client being connected, so this is established first.

## Steps

### Create `src/Hermod.Api/Discord/BotService.cs`

```csharp
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Discord;

public class BotService(
    DiscordSocketClient client,
    ILogger<BotService> logger)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Log += LogAsync;
        Client.Ready += OnReadyAsync;

        await Client.StartAsync();
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task LogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error    => LogLevel.Error,
            LogSeverity.Warning  => LogLevel.Warning,
            LogSeverity.Info     => LogLevel.Information,
            LogSeverity.Verbose  => LogLevel.Debug,
            LogSeverity.Debug    => LogLevel.Trace,
            _                    => LogLevel.Information,
        };

        logger.Log(level, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        logger.LogInformation(
            "Connected as {Username}#{Discriminator} serving {GuildCount} guild(s)",
            Client.CurrentUser.Username,
            Client.CurrentUser.Discriminator,
            Client.Guilds.Count);
        return Task.CompletedTask;
    }
}
```

### Register in `src/Hermod.Api/Program.cs`

After the `AddDiscordHost` / `UseInteractionService` block, add:

```csharp
builder.Services.AddHostedService<BotService>();
```

## Notes
- `DiscordClientService` is the base class from `Discord.Addons.Hosting`. It exposes `Client`
  (the `DiscordSocketClient`) and `Logger` (an `ILogger`).
- `Client.StartAsync()` begins the WebSocket connection but does not block — `Ready` fires
  asynchronously once the connection is established and guild data is downloaded.
- `Task.Delay(Timeout.Infinite, stoppingToken)` keeps the service alive until the host shuts
  down, at which point the cancellation triggers `Client.StopAsync()` (handled by the base).
- Do not call `Client.LoginAsync` here — `Discord.Addons.Hosting` handles login internally
  using the token provided in `AddDiscordHost`.

## Acceptance Criteria
- [ ] `src/Hermod.Api/Discord/BotService.cs` exists
- [ ] App starts without error when a valid token is configured
- [ ] Startup logs include: `Connected as <BotName>#<Discriminator> serving N guild(s)`
- [ ] Discord log messages (from Discord.Net internals) appear in the app's log output
- [ ] Bot appears with online status in Discord
