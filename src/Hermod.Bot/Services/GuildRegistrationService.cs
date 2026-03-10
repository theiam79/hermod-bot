using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hermod.Bot.Services;

public class GuildRegistrationService(
    BotDbContext db,
    IMessageBus bus,
    ILogger<GuildRegistrationService> logger)
{
    public async Task<GuildMappingEntity> RegisterGuildAsync(ulong discordGuildId, string guildName)
    {
        logger.LogInformation("Registering guild {GuildName} ({GuildId}) via NATS...", guildName, discordGuildId);

        var registered = await bus.InvokeAsync<CommunityRegistered>(
            new RegisterCommunity(Providers.Discord, discordGuildId.ToString(), guildName),
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
            existing = new GuildMappingEntity
            {
                Id = Guid.NewGuid(),
                DiscordGuildId = discordGuildId,
                GroupId = registered.GroupId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.GuildMappings.Add(existing);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Registered guild {GuildName} ({GuildId}) → Group {GroupId}", guildName, discordGuildId, registered.GroupId);

        return existing;
    }

    public async Task ReactivateAsync(GuildMappingEntity mapping)
    {
        mapping.IsActive = true;
        mapping.DeactivatedAt = null;
        await db.SaveChangesAsync();

        if (mapping.PostChannelId is not null)
        {
            await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, true));
        }

        logger.LogInformation("Reactivated guild mapping for DiscordGuildId {GuildId} → Group {GroupId}", mapping.DiscordGuildId, mapping.GroupId);
    }

    public async Task DeactivateAsync(GuildMappingEntity mapping)
    {
        mapping.IsActive = false;
        mapping.DeactivatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, false));

        logger.LogInformation("Deactivated guild mapping for DiscordGuildId {GuildId}", mapping.DiscordGuildId);
    }
}
