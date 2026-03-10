using Hermod.Bot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

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
            var registrationService = scope.ServiceProvider.GetRequiredService<GuildRegistrationService>();

            var existing = await db.GuildMappings
                .FirstOrDefaultAsync(m => m.DiscordGuildId == guild.Id);

            if (existing is not null)
            {
                if (!existing.IsActive)
                {
                    await registrationService.ReactivateAsync(existing);
                }

                return;
            }

            await registrationService.RegisterGuildAsync(guild.Id, guild.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling GuildCreate for {GuildName} ({GuildId})", guild.Name, guild.Id);
        }
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
            var registrationService = scope.ServiceProvider.GetRequiredService<GuildRegistrationService>();

            var mapping = await db.GuildMappings
                .FirstOrDefaultAsync(m => m.DiscordGuildId == args.GuildId);

            if (mapping is null)
            {
                logger.LogWarning("No guild mapping found for left guild ({GuildId})", args.GuildId);
                return;
            }

            await registrationService.DeactivateAsync(mapping);
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
        var registrationService = scope.ServiceProvider.GetRequiredService<GuildRegistrationService>();

        var connectedGuildIds = client.Cache.Guilds.Keys.ToHashSet();
        logger.LogInformation("Guild sync: {ConnectedCount} connected guild(s), checking mappings...", connectedGuildIds.Count);
        var allMappings = await db.GuildMappings.ToListAsync();
        var mappedGuildIds = allMappings.ToDictionary(m => m.DiscordGuildId);
        logger.LogInformation("Guild sync: {MappedCount} existing mapping(s)", allMappings.Count);

        // Register guilds the bot is in but has no mapping for, or reactivate inactive ones
        foreach (var (guildId, guild) in client.Cache.Guilds)
        {
            if (mappedGuildIds.TryGetValue(guildId, out var mapping))
            {
                if (!mapping.IsActive)
                {
                    await registrationService.ReactivateAsync(mapping);
                }
            }
            else
            {
                await registrationService.RegisterGuildAsync(guildId, guild.Name);
            }
        }
    }
}
