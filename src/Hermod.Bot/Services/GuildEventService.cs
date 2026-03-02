using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using Wolverine;

namespace Hermod.Bot.Services;

public class GuildCreateHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<GuildCreateHandler> logger) : IGuildCreateGatewayHandler
{
    public async ValueTask HandleAsync(GuildCreateEventArgs args)
    {
        if (args.Guild is not { } guild)
            return;

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

                    if (existing.PostChannelId is not null)
                    {
                        await bus.PublishAsync(new UpdateGroupSharing(existing.GroupId, true));
                    }

                    logger.LogInformation("Reactivated guild mapping for {GuildName} ({GuildId})", guild.Name, guild.Id);
                }

                return;
            }

            await RegisterNewGuildAsync(guild.Id, guild.Name, db, bus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling GuildCreate for {GuildName} ({GuildId})", guild.Name, guild.Id);
        }
    }

    private async Task RegisterNewGuildAsync(ulong discordGuildId, string guildName, BotDbContext db, IMessageBus bus)
    {
        logger.LogInformation("Registering guild {GuildName} ({GuildId}) via NATS...", guildName, discordGuildId);
        var registered = await bus.InvokeAsync<CommunityRegistered>(
            new RegisterCommunity("Discord", discordGuildId.ToString(), guildName),
            timeout: TimeSpan.FromSeconds(10));

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

public class GuildDeleteHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<GuildDeleteHandler> logger) : IGuildDeleteGatewayHandler
{
    public async ValueTask HandleAsync(GuildDeleteEventArgs args)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var mapping = await db.GuildMappings
                .FirstOrDefaultAsync(m => m.DiscordGuildId == args.GuildId);

            if (mapping is null)
            {
                logger.LogWarning("No guild mapping found for left guild ({GuildId})", args.GuildId);
                return;
            }

            mapping.IsActive = false;
            mapping.DeactivatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, false));
            logger.LogInformation("Deactivated guild mapping for guild ({GuildId})", args.GuildId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling GuildDelete for guild ({GuildId})", args.GuildId);
        }
    }
}

public class ReadyHandler(
    IServiceScopeFactory scopeFactory,
    GatewayClient gateway,
    ILogger<ReadyHandler> logger) : IReadyGatewayHandler
{
    public async ValueTask HandleAsync(ReadyEventArgs args)
    {
        try
        {
            logger.LogInformation("Discord client ready. Syncing guilds...");
            await SyncGuildsAsync(gateway);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Guild sync failed during startup — guilds will be registered on next join event");
        }
    }

    private async Task SyncGuildsAsync(GatewayClient client)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var connectedGuildIds = client.Cache.Guilds.Keys.ToHashSet();
        logger.LogInformation("Guild sync: {ConnectedCount} connected guild(s), checking mappings...", connectedGuildIds.Count);
        var allMappings = await db.GuildMappings.ToListAsync();
        var mappedGuildIds = allMappings.ToDictionary(m => m.DiscordGuildId);
        logger.LogInformation("Guild sync: {MappedCount} existing mapping(s)", allMappings.Count);

        // Register guilds the bot is in but has no mapping for
        foreach (var (guildId, guild) in client.Cache.Guilds)
        {
            if (mappedGuildIds.TryGetValue(guildId, out var mapping))
            {
                if (!mapping.IsActive)
                {
                    mapping.IsActive = true;
                    mapping.DeactivatedAt = null;

                    if (mapping.PostChannelId is not null)
                    {
                        await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, true));
                    }

                    logger.LogInformation("Reactivated guild mapping during sync for {GuildName} ({GuildId})", guild.Name, guildId);
                }
            }
            else
            {
                await RegisterNewGuildAsync(guildId, guild.Name, db, bus);
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
        logger.LogInformation("Registering guild {GuildName} ({GuildId}) via NATS...", guildName, discordGuildId);
        var registered = await bus.InvokeAsync<CommunityRegistered>(
            new RegisterCommunity("Discord", discordGuildId.ToString(), guildName),
            timeout: TimeSpan.FromSeconds(10));

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
