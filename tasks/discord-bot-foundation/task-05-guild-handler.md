# Task 05 — GuildHandler: Guild Sync

## Why
`GuildHandler` keeps the `Groups` table in sync with the Discord servers the bot belongs to.
On startup it upserts every guild the bot is currently in. When the bot joins a new guild, it
upserts that guild too. When the bot leaves a guild, it logs the event but does NOT delete the
row — historical play data remains intact.

## Steps

### Create `src/Hermod.Api/Discord/GuildHandler.cs`

```csharp
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Discord;

public class GuildHandler(
    DiscordSocketClient client,
    ILogger<GuildHandler> logger,
    IServiceScopeFactory scopeFactory)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Ready += OnReadyAsync;
        Client.JoinedGuild += OnJoinedGuildAsync;
        Client.LeftGuild += OnLeftGuildAsync;

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnReadyAsync()
    {
        foreach (var guild in Client.Guilds)
            await SyncGuildAsync(guild);

        Logger.LogInformation("Guild sync complete — {Count} guild(s) upserted", Client.Guilds.Count);
    }

    private Task OnJoinedGuildAsync(SocketGuild guild)
        => SyncGuildAsync(guild);

    private Task OnLeftGuildAsync(SocketGuild guild)
    {
        Logger.LogInformation("Left guild {Name} ({Id}) — row retained", guild.Name, guild.Id);
        return Task.CompletedTask;
    }

    private async Task SyncGuildAsync(IGuild guild)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();

        var existing = await db.Groups
            .FirstOrDefaultAsync(g => g.DiscordGuildId == guild.Id);

        if (existing is null)
        {
            db.Groups.Add(new GroupEntity
            {
                Id = GroupId.From(Guid.NewGuid()),
                Name = guild.Name,
                DiscordGuildId = guild.Id,
                DiscordPostChannelId = null,
                AllowSharing = false   // explicit opt-in required
            });
            Logger.LogInformation("Added new guild {Name} ({Id})", guild.Name, guild.Id);
        }
        else
        {
            existing.Name = guild.Name;   // keep name in sync if it changes
        }

        await db.SaveChangesAsync();
    }
}
```

### Register in `src/Hermod.Api/Program.cs`

After `builder.Services.AddHostedService<InteractionHandler>();`, add:

```csharp
builder.Services.AddHostedService<GuildHandler>();
```

## Notes
- `IServiceScopeFactory` is required because `HermodContext` is a scoped service and
  `GuildHandler` is a singleton (all `IHostedService` instances are singletons). Always
  create a new scope per database operation; never inject `HermodContext` directly.
- `AllowSharing = false` is intentional — a server admin must explicitly run a slash command
  (implemented in a future batch) to opt in to play sharing.
- `GroupEntity.AllowSharing` defaults to `true` in the entity class definition, so it must be
  explicitly set to `false` here when creating new rows.
- `DiscordPostChannelId = null` — the posting channel is configured later via slash command.
- On `Client.LeftGuild`, the row is kept so historical play data for that guild remains
  queryable. A future admin command can mark the group as inactive if needed.
- `SaveChangesAsync` is called per guild to avoid losing partial progress if one upsert fails.

## Acceptance Criteria
- [ ] `src/Hermod.Api/Discord/GuildHandler.cs` exists
- [ ] `dotnet build src/Hermod.Api/Hermod.Api.csproj` succeeds
- [ ] App startup logs show `Guild sync complete — N guild(s) upserted`
- [ ] After startup, the `Groups` table contains one row per guild the bot is in
- [ ] Each new row has `AllowSharing = false` and `DiscordPostChannelId = null`
- [ ] Joining the bot to a new server adds a row with `AllowSharing = false`
- [ ] Leaving a server logs a message but does not delete the row
