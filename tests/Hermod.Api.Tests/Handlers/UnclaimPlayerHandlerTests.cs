using Hermod.Api.Features.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class UnclaimPlayerHandlerTests
{
    private static HermodContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    private static async Task<(HermodContext db, Guid userId, string playerUuid, Guid playId)>
        SetupUserWithClaim()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var playerUuid = Guid.NewGuid().ToString();
        var playId = Guid.NewGuid();

        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "TestUser",
        });

        var playEntity = new PlayEntity
        {
            Id = PlayId.From(playId),
            UploadedById = UserId.From(Guid.NewGuid()),
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.Plays.Add(playEntity);

        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(playId),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
            MappedUserId = UserId.From(userId),
        });

        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = playerUuid,
            MappedUserId = UserId.From(userId),
        });

        await db.SaveChangesAsync();
        return (db, userId, playerUuid, playId);
    }

    [Test]
    public async Task Unclaim_RemovesPlayerMapping()
    {
        var (db, userId, playerUuid, _) = await SetupUserWithClaim();

        await UnclaimPlayerHandler.Handle(
            new UnclaimPlayer(userId, playerUuid), db);
        await db.SaveChangesAsync();

        var mappingCount = await db.PlayerMappings.CountAsync();
        await Assert.That(mappingCount).IsEqualTo(0);
    }

    [Test]
    public async Task Unclaim_ClearsMappedUserIdOnAllAffectedPlayPlayers()
    {
        var (db, userId, playerUuid, playId) = await SetupUserWithClaim();

        // Add a second play with the same player UUID, also claimed
        var play2Id = Guid.NewGuid();
        db.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(play2Id),
            UploadedById = UserId.From(Guid.NewGuid()),
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Wingspan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(play2Id),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
            MappedUserId = UserId.From(userId),
        });
        await db.SaveChangesAsync();

        await UnclaimPlayerHandler.Handle(
            new UnclaimPlayer(userId, playerUuid), db);
        await db.SaveChangesAsync();

        var players = await db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == playerUuid)
            .ToListAsync();

        await Assert.That(players).Count().IsEqualTo(2);
        foreach (var p in players)
            await Assert.That(p.MappedUserId).IsNull();
    }

    [Test]
    public async Task Unclaim_OtherUser_ReturnsForbidden()
    {
        var (db, _, playerUuid, _) = await SetupUserWithClaim();

        var otherUserId = Guid.NewGuid();
        var (result, claimChanged) = await UnclaimPlayerHandler.Handle(
            new UnclaimPlayer(otherUserId, playerUuid), db);

        await Assert.That(result.Status).IsEqualTo(UnclaimStatus.Forbidden);
        await Assert.That(claimChanged).IsNull();
    }

    [Test]
    public async Task Unclaim_NonExistentMapping_ReturnsNotFound()
    {
        var db = CreateDb();

        var (result, claimChanged) = await UnclaimPlayerHandler.Handle(
            new UnclaimPlayer(Guid.NewGuid(), Guid.NewGuid().ToString()), db);

        await Assert.That(result.Status).IsEqualTo(UnclaimStatus.NotFound);
        await Assert.That(claimChanged).IsNull();
    }

    [Test]
    public async Task Unclaim_Success_ReturnsClaimChangedCascade()
    {
        var (db, userId, playerUuid, _) = await SetupUserWithClaim();

        var (result, claimChanged) = await UnclaimPlayerHandler.Handle(
            new UnclaimPlayer(userId, playerUuid), db);

        await Assert.That(result.Status).IsEqualTo(UnclaimStatus.Removed);
        await Assert.That(claimChanged).IsNotNull();
        await Assert.That(claimChanged!.BgStatsPlayerUuid).IsEqualTo(playerUuid);
        await Assert.That(claimChanged.MappedUserId).IsNull();
    }
}
