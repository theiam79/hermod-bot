# Task 04 — InteractionHandler: Slash Command Routing

## Why
`InteractionHandler` wires Discord's interaction events to the `InteractionService` pipeline and
registers slash commands globally on startup. It also provides the first concrete slash command
(`/hermod ping`) to confirm the end-to-end flow works.

## Steps

### Create `src/Hermod.Api/Discord/InteractionHandler.cs`

```csharp
using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Discord;

public class InteractionHandler(
    DiscordSocketClient client,
    ILogger<InteractionHandler> logger,
    InteractionService interactionService,
    IServiceProvider services)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Ready += OnReadyAsync;
        Client.InteractionCreated += OnInteractionCreatedAsync;
        interactionService.SlashCommandExecuted += OnSlashCommandExecutedAsync;

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnReadyAsync()
    {
        await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        await interactionService.RegisterCommandsGloballyAsync();
        Logger.LogInformation("Registered {Count} slash command module(s) globally",
            interactionService.Modules.Count);
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(Client, interaction);
        await interactionService.ExecuteCommandAsync(ctx, services);
    }

    private Task OnSlashCommandExecutedAsync(
        SlashCommandInfo command,
        IInteractionContext context,
        IResult result)
    {
        if (!result.IsSuccess)
            Logger.LogWarning("Slash command /{Name} failed: {Error}", command.Name, result.ErrorReason);

        return Task.CompletedTask;
    }
}
```

### Create `src/Hermod.Api/Discord/Modules/InfoModule.cs`

```csharp
using Discord.Interactions;

namespace Hermod.Api.Discord.Modules;

[Group("hermod", "Hermod bot commands")]
public class InfoModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Check if the bot is alive")]
    public async Task PingAsync() => await RespondAsync("Pong!");
}
```

### Register in `src/Hermod.Api/Program.cs`

After `builder.Services.AddHostedService<BotService>();`, add:

```csharp
builder.Services.AddHostedService<InteractionHandler>();
```

## Notes
- `AddModulesAsync` scans the entry assembly for all `InteractionModuleBase<T>` subclasses.
  `InfoModule` will be discovered automatically — no manual registration needed.
- `RegisterCommandsGloballyAsync` pushes commands to Discord's API. Global propagation can take
  up to one hour the first time; subsequent updates are faster.
- For faster iteration during development, consider `RegisterCommandsToGuildAsync(guildId)` in
  a dev environment. Global registration is correct for production use.
- `IServiceProvider services` is injected so `InteractionService.ExecuteCommandAsync` can resolve
  scoped dependencies inside module constructors.
- `Client.StartAsync()` is NOT called here — `BotService` (Task 03) owns the connection lifecycle.

## Acceptance Criteria
- [ ] `src/Hermod.Api/Discord/InteractionHandler.cs` exists
- [ ] `src/Hermod.Api/Discord/Modules/InfoModule.cs` exists
- [ ] `dotnet build src/Hermod.Api/Hermod.Api.csproj` succeeds
- [ ] App startup logs show `Registered N slash command module(s) globally`
- [ ] `/hermod ping` in Discord responds `Pong!`
