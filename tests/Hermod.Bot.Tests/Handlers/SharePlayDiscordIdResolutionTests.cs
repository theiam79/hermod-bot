using Hermod.Bot.Data;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Bot.Tests.Handlers;

public class SharePlayDiscordIdResolutionTests
{
    private static BotDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BotDbContext(options);
    }

    [Test]
    public async Task ResolvesDiscordIdsFromMappedUserIds()
    {
        var db = CreateDb();

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var unmappedUserId = Guid.NewGuid();
        var discord1 = 111111111111111111UL;
        var discord2 = 222222222222222222UL;

        db.DiscordUserMappings.AddRange(
            new DiscordUserMappingEntity
            {
                Id = Guid.NewGuid(),
                HermodUserId = user1Id,
                DiscordUserId = discord1,
                CreatedAt = DateTime.UtcNow,
            },
            new DiscordUserMappingEntity
            {
                Id = Guid.NewGuid(),
                HermodUserId = user2Id,
                DiscordUserId = discord2,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        // Simulate the handler's resolution logic: only look up mapped user IDs
        var mappedUserIds = new List<Guid> { user1Id, user2Id, unmappedUserId };

        var discordUserIds = await db.DiscordUserMappings
            .Where(m => mappedUserIds.Contains(m.HermodUserId))
            .ToDictionaryAsync(m => m.HermodUserId, m => m.DiscordUserId);

        await Assert.That(discordUserIds).Count().IsEqualTo(2);
        await Assert.That(discordUserIds[user1Id]).IsEqualTo(discord1);
        await Assert.That(discordUserIds[user2Id]).IsEqualTo(discord2);
        await Assert.That(discordUserIds.ContainsKey(unmappedUserId)).IsFalse();
    }
}
