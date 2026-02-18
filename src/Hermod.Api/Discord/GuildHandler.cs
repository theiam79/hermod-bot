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
        {
            await SyncGuildAsync(guild);
        }

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
        try
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
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to sync guild {Name} ({Id})", guild.Name, guild.Id);
        }
    }
}
