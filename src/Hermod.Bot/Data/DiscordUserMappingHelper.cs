using Microsoft.EntityFrameworkCore;

namespace Hermod.Bot.Data;

public static class DiscordUserMappingHelper
{
    public static async Task UpsertAsync(BotDbContext db, ulong discordUserId, Guid hermodUserId)
    {
        var existing = await db.DiscordUserMappings
            .FirstOrDefaultAsync(m => m.HermodUserId == hermodUserId || m.DiscordUserId == discordUserId);

        if (existing is null)
        {
            db.DiscordUserMappings.Add(new DiscordUserMappingEntity
            {
                Id = Guid.NewGuid(),
                DiscordUserId = discordUserId,
                HermodUserId = hermodUserId,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.DiscordUserId = discordUserId;
            existing.HermodUserId = hermodUserId;
        }

        await db.SaveChangesAsync();
    }
}
