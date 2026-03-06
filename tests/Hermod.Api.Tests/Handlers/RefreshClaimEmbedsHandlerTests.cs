using Hermod.Api.Features.Plays;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class RefreshClaimEmbedsHandlerTests
{
    private static HermodContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    [Test]
    public async Task ClaimChanged_EmitsSharePlayToGroupForAffectedPlays()
    {
        var db = CreateDb();
        var userId = UserId.From(Guid.NewGuid());
        var playerUuid = Guid.NewGuid().ToString();
        var playId = Guid.NewGuid();
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity { Id = userId, DisplayName = "Claimer" });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "TestGroup", AllowSharing = true });
        db.UserGroups.Add(new UserGroupEntity { UserId = userId, GroupId = groupId, Role = GroupRole.Member });

        var play = new PlayEntity
        {
            Id = PlayId.From(playId),
            UploadedById = userId,
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.Plays.Add(play);
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(playId),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
            MappedUserId = userId,
        });
        await db.SaveChangesAsync();

        var result = await RefreshClaimEmbedsHandler.Handle(
            new ClaimChanged(playerUuid, userId.Value), db);

        await Assert.That(result).Count().IsEqualTo(1);
        var msg = result.OfType<SharePlayToGroup>().Single();
        await Assert.That(msg.PlayId).IsEqualTo(playId);
        await Assert.That(msg.GroupId).IsEqualTo(groupId.Value);
        await Assert.That(msg.ChangeType).IsEqualTo(PlayChangeType.Updated);
        await Assert.That(msg.Snapshot.GameName).IsEqualTo("Catan");
    }

    [Test]
    public async Task ClaimChanged_SnapshotIncludesUpdatedMappedUserId()
    {
        var db = CreateDb();
        var claimerId = UserId.From(Guid.NewGuid());
        var playerUuid = Guid.NewGuid().ToString();
        var playId = Guid.NewGuid();
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity { Id = claimerId, DisplayName = "Claimer" });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "TestGroup", AllowSharing = true });
        db.UserGroups.Add(new UserGroupEntity { UserId = claimerId, GroupId = groupId, Role = GroupRole.Member });

        db.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(playId),
            UploadedById = claimerId,
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(playId),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
            MappedUserId = claimerId,
        });
        await db.SaveChangesAsync();

        var result = await RefreshClaimEmbedsHandler.Handle(
            new ClaimChanged(playerUuid, claimerId.Value), db);

        var msg = result.OfType<SharePlayToGroup>().Single();
        var player = msg.Snapshot.Players.Single(p => p.BgStatsPlayerUuid == playerUuid);
        await Assert.That(player.MappedUserId).IsEqualTo(claimerId.Value);
    }

    [Test]
    public async Task ClaimChanged_DoesNotTouchUnrelatedPlays()
    {
        var db = CreateDb();
        var userId = UserId.From(Guid.NewGuid());
        var playerUuid = Guid.NewGuid().ToString();
        var otherUuid = Guid.NewGuid().ToString();
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity { Id = userId, DisplayName = "User" });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "TestGroup", AllowSharing = true });
        db.UserGroups.Add(new UserGroupEntity { UserId = userId, GroupId = groupId, Role = GroupRole.Member });

        // Play with the affected UUID
        db.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(Guid.NewGuid()),
            UploadedById = userId,
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = db.Plays.Local.First().Id,
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
        });

        // Play WITHOUT the affected UUID
        var unrelatedPlayId = PlayId.From(Guid.NewGuid());
        db.Plays.Add(new PlayEntity
        {
            Id = unrelatedPlayId,
            UploadedById = userId,
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Wingspan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = unrelatedPlayId,
            BgStatsPlayerUuid = otherUuid,
            PlayerName = "Bob",
        });
        await db.SaveChangesAsync();

        var result = await RefreshClaimEmbedsHandler.Handle(
            new ClaimChanged(playerUuid, userId.Value), db);

        // Only the play with the affected UUID should be re-shared
        var playIds = result.OfType<SharePlayToGroup>().Select(m => m.PlayId).ToList();
        await Assert.That(playIds).Count().IsEqualTo(1);
        await Assert.That(playIds).DoesNotContain(unrelatedPlayId.Value);
    }

    [Test]
    public async Task ClaimChanged_NoAffectedPlays_ReturnsEmpty()
    {
        var db = CreateDb();

        var result = await RefreshClaimEmbedsHandler.Handle(
            new ClaimChanged(Guid.NewGuid().ToString(), Guid.NewGuid()), db);

        await Assert.That(result).Count().IsEqualTo(0);
    }
}
