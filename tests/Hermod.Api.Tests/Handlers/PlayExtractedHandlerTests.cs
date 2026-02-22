using Hermod.Api.Handlers;
using Hermod.Api.Messages;
using Hermod.BGStats.Models;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class PlayExtractedHandlerTests
{
    private static HermodContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    private static Play CreateTestPlay(Guid? uuid = null, string gameName = "Catan") => new()
    {
        Uuid = uuid ?? Guid.NewGuid(),
        Game = new Game { Uuid = Guid.NewGuid(), Name = gameName },
        Location = new Location { Uuid = Guid.NewGuid(), Name = "Home" },
        DatePlayed = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        Duration = TimeSpan.FromMinutes(60),
        Scores =
        [
            new Score
            {
                Player = new Player { Uuid = Guid.NewGuid(), Name = "Alice" },
                ScoreExpression = "10",
                Winner = true,
                Rank = 1,
            },
        ],
    };

    [Test]
    public async Task Create_NewPlay_ReturnsCreated()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var play = CreateTestPlay();
        var message = new PlayExtracted(play, null, Guid.NewGuid(), userId);

        var result = await PlayExtractedHandler.Handle(message, db);

        await Assert.That(result.ChangeType).IsEqualTo(PlayChangeType.Created);
        await Assert.That(result.UploadedById).IsEqualTo(userId);
        await Assert.That(result.PlayId).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Create_NewPlay_AddsEntityToContext()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var play = CreateTestPlay();
        var message = new PlayExtracted(play, null, Guid.NewGuid(), userId);

        await PlayExtractedHandler.Handle(message, db);
        await db.SaveChangesAsync();

        var count = await db.Plays.CountAsync();
        await Assert.That(count).IsEqualTo(1);

        var entity = await db.Plays.Include(p => p.Players).FirstAsync();
        await Assert.That(entity.GameName).IsEqualTo("Catan");
        await Assert.That(entity.Players).Count().IsEqualTo(1);
        await Assert.That(entity.UploadedById).IsEqualTo(UserId.From(userId));
    }

    [Test]
    public async Task Update_ExistingPlay_ReturnsUpdated()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var playUuid = Guid.NewGuid();

        // Seed existing play
        var existingId = PlayId.From(Guid.NewGuid());
        db.Plays.Add(new PlayEntity
        {
            Id = existingId,
            UploadedById = UserId.From(userId),
            BgStatsPlayUuid = playUuid.ToString(),
            GameName = "Old Name",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = existingId,
            BgStatsPlayerUuid = Guid.NewGuid().ToString(),
            PlayerName = "OldPlayer",
        });
        await db.SaveChangesAsync();

        var play = CreateTestPlay(playUuid, "New Name");
        var message = new PlayExtracted(play, null, Guid.NewGuid(), userId);

        var result = await PlayExtractedHandler.Handle(message, db);

        await Assert.That(result.ChangeType).IsEqualTo(PlayChangeType.Updated);
    }

    [Test]
    public async Task Update_ExistingPlay_UpdatesFieldsAndReplacesPlayers()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var playUuid = Guid.NewGuid();
        var existingPlayId = PlayId.From(Guid.NewGuid());

        db.Plays.Add(new PlayEntity
        {
            Id = existingPlayId,
            UploadedById = UserId.From(userId),
            BgStatsPlayUuid = playUuid.ToString(),
            GameName = "Old Name",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = existingPlayId,
            BgStatsPlayerUuid = Guid.NewGuid().ToString(),
            PlayerName = "OldPlayer",
        });
        await db.SaveChangesAsync();

        var play = CreateTestPlay(playUuid, "New Name");
        var message = new PlayExtracted(play, null, Guid.NewGuid(), userId);

        var result = await PlayExtractedHandler.Handle(message, db);
        await db.SaveChangesAsync();

        await Assert.That(result.PlayId).IsEqualTo(existingPlayId.Value);

        var entity = await db.Plays.Include(p => p.Players).FirstAsync();
        await Assert.That(entity.GameName).IsEqualTo("New Name");
        await Assert.That(entity.Players).Count().IsEqualTo(1);
        await Assert.That(entity.Players[0].PlayerName).IsEqualTo("Alice");
    }

    [Test]
    public async Task Update_DifferentUser_SameUuid_CreatesNewPlay()
    {
        await using var db = CreateInMemoryDb();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var playUuid = Guid.NewGuid();

        db.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(Guid.NewGuid()),
            UploadedById = UserId.From(user1),
            BgStatsPlayUuid = playUuid.ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var play = CreateTestPlay(playUuid);
        var message = new PlayExtracted(play, null, null, user2);

        var result = await PlayExtractedHandler.Handle(message, db);
        await db.SaveChangesAsync();

        await Assert.That(result.ChangeType).IsEqualTo(PlayChangeType.Created);
        var count = await db.Plays.CountAsync();
        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task Create_ResolvesPlayerMappings()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var mappedUserId = UserId.From(Guid.NewGuid());
        var playerUuid = Guid.NewGuid();

        var play = new Play
        {
            Uuid = Guid.NewGuid(),
            Game = new Game { Uuid = Guid.NewGuid(), Name = "Catan" },
            Location = new Location { Uuid = Guid.NewGuid(), Name = "Home" },
            DatePlayed = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Scores =
            [
                new Score
                {
                    Player = new Player { Uuid = playerUuid, Name = "Alice" },
                    ScoreExpression = "10",
                    Winner = true,
                    Rank = 1,
                },
            ],
        };

        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = playerUuid.ToString(),
            MappedUserId = mappedUserId,
        });
        await db.SaveChangesAsync();

        var message = new PlayExtracted(play, null, null, userId);
        await PlayExtractedHandler.Handle(message, db);
        await db.SaveChangesAsync();

        var entity = await db.Plays.Include(p => p.Players).FirstAsync();
        await Assert.That(entity.Players[0].MappedUserId).IsEqualTo(mappedUserId);
    }
}
