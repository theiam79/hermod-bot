using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hermod.Bot.Services;

public class GuildEventService(
    DiscordSocketClient client,
    ILogger<GuildEventService> logger,
    IServiceScopeFactory scopeFactory) : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Client.WaitForReadyAsync(stoppingToken);

        Client.JoinedGuild += OnJoinedGuildAsync;
        Client.LeftGuild += OnLeftGuildAsync;

        try
        {
            logger.LogInformation("Discord client ready. Syncing guilds...");
            await SyncGuildsAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Guild sync failed during startup — guilds will be registered on next join event");
        }
    }

    private async Task OnJoinedGuildAsync(SocketGuild guild)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var existing = await db.GuildMappings
                .FirstOrDefaultAsync(m => m.DiscordGuildId == guild.Id);

            if (existing is not null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    existing.DeactivatedAt = null;
                    await db.SaveChangesAsync();
                    await bus.PublishAsync(new UpdateGroupSharing(existing.GroupId, true));
                    logger.LogInformation("Reactivated guild mapping for {GuildName} ({GuildId})", guild.Name, guild.Id);
                }

                return;
            }

            await RegisterNewGuildAsync(guild.Id, guild.Name, db, bus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling JoinedGuild for {GuildName} ({GuildId})", guild.Name, guild.Id);
        }
    }

    private async Task OnLeftGuildAsync(SocketGuild guild)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var mapping = await db.GuildMappings
                .FirstOrDefaultAsync(m => m.DiscordGuildId == guild.Id);

            if (mapping is null)
            {
                logger.LogWarning("No guild mapping found for left guild {GuildName} ({GuildId})", guild.Name, guild.Id);
                return;
            }

            mapping.IsActive = false;
            mapping.DeactivatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, false));
            logger.LogInformation("Deactivated guild mapping for {GuildName} ({GuildId})", guild.Name, guild.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling LeftGuild for {GuildName} ({GuildId})", guild.Name, guild.Id);
        }
    }

    private async Task SyncGuildsAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var connectedGuildIds = Client.Guilds.Select(g => g.Id).ToHashSet();
        var allMappings = await db.GuildMappings.ToListAsync();
        var mappedGuildIds = allMappings.ToDictionary(m => m.DiscordGuildId);

        // Register guilds the bot is in but has no mapping for
        foreach (var guild in Client.Guilds)
        {
            if (mappedGuildIds.TryGetValue(guild.Id, out var mapping))
            {
                if (!mapping.IsActive)
                {
                    mapping.IsActive = true;
                    mapping.DeactivatedAt = null;
                    await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, true));
                    logger.LogInformation("Reactivated guild mapping during sync for {GuildName} ({GuildId})", guild.Name, guild.Id);
                }
            }
            else
            {
                await RegisterNewGuildAsync(guild.Id, guild.Name, db, bus);
            }
        }

        // Deactivate mappings for guilds the bot is no longer in
        foreach (var mapping in allMappings.Where(m => m.IsActive && !connectedGuildIds.Contains(m.DiscordGuildId)))
        {
            mapping.IsActive = false;
            mapping.DeactivatedAt = DateTime.UtcNow;
            await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, false));
            logger.LogInformation("Deactivated orphaned guild mapping for DiscordGuildId {GuildId}", mapping.DiscordGuildId);
        }

        await db.SaveChangesAsync();
    }

    private async Task RegisterNewGuildAsync(ulong discordGuildId, string guildName, BotDbContext db, IMessageBus bus)
    {
        var registered = await bus.InvokeAsync<GuildRegistered>(
            new RegisterGuild(discordGuildId, guildName),
            timeout: TimeSpan.FromSeconds(30));

        var existing = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == discordGuildId);

        if (existing is not null)
        {
            existing.GroupId = registered.GroupId;
            existing.IsActive = true;
            existing.DeactivatedAt = null;
        }
        else
        {
            db.GuildMappings.Add(new GuildMappingEntity
            {
                Id = Guid.NewGuid(),
                DiscordGuildId = discordGuildId,
                GroupId = registered.GroupId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Registered guild {GuildName} ({GuildId}) → Group {GroupId}", guildName, discordGuildId, registered.GroupId);
    }
}
