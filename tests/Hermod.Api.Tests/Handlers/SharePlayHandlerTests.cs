using Hermod.Api.Handlers;
using Hermod.Api.Messages;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class SharePlayHandlerTests
{
    private static HermodContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    private static PlayEntity CreatePlay(PlayId playId, UserId uploadedById) => new()
    {
        Id = playId,
        UploadedById = uploadedById,
        BgStatsPlayUuid = Guid.NewGuid().ToString(),
        GameName = "Wingspan",
        DatePlayed = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        Duration = TimeSpan.FromMinutes(90),
        LocationName = "Home",
        Rounds = 4,
        Comments = "Great game!",
        GameThumbnailUrl = "https://example.com/wingspan.jpg",
        BggGameId = 266192,
        CreatedAt = DateTime.UtcNow,
        Players =
        [
            new PlayPlayerEntity
            {
                Id = PlayPlayerId.From(Guid.NewGuid()),
                PlayId = playId,
                BgStatsPlayerUuid = Guid.NewGuid().ToString(),
                PlayerName = "Alice",
                Score = "85",
                CalculatedScore = 85.0,
                Winner = true,
                Rank = 1,
            },
            new PlayPlayerEntity
            {
                Id = PlayPlayerId.From(Guid.NewGuid()),
                PlayId = playId,
                BgStatsPlayerUuid = Guid.NewGuid().ToString(),
                PlayerName = "Bob",
                Score = "72",
                CalculatedScore = 72.0,
                Winner = false,
                Rank = 2,
            },
        ],
    };

    [Test]
    public async Task UserInTwoGroups_OneSharingEnabled_EmitsOneMessage()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var playId = PlayId.From(Guid.NewGuid());
        var sharingGroupId = GroupId.From(Guid.NewGuid());
        var nonSharingGroupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "Test User",
        });
        db.Groups.Add(new GroupEntity { Id = sharingGroupId, Name = "Sharing Group", AllowSharing = true });
        db.Groups.Add(new GroupEntity { Id = nonSharingGroupId, Name = "Non-Sharing Group", AllowSharing = false });
        db.UserGroups.Add(new UserGroupEntity { UserId = UserId.From(userId), GroupId = sharingGroupId });
        db.UserGroups.Add(new UserGroupEntity { UserId = UserId.From(userId), GroupId = nonSharingGroupId });
        db.Plays.Add(CreatePlay(playId, UserId.From(userId)));
        await db.SaveChangesAsync();

        var message = new PlayPersisted(playId.Value, userId, PlayChangeType.Created);
        var result = await SharePlayHandler.Handle(message, db);

        await Assert.That(result).Count().IsEqualTo(1);

        var shareMsg = result.OfType<SharePlayToGroup>().Single();
        await Assert.That(shareMsg.GroupId).IsEqualTo(sharingGroupId.Value);
        await Assert.That(shareMsg.PlayId).IsEqualTo(message.PlayId);
        await Assert.That(shareMsg.ChangeType).IsEqualTo(PlayChangeType.Created);
        await Assert.That(shareMsg.Snapshot.GameName).IsEqualTo("Wingspan");
        await Assert.That(shareMsg.Snapshot.Players).Count().IsEqualTo(2);
    }

    [Test]
    public async Task UserInNoGroups_EmitsNothing()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        var message = new PlayPersisted(Guid.NewGuid(), userId, PlayChangeType.Created);
        var result = await SharePlayHandler.Handle(message, db);

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UserInGroups_NoneSharing_EmitsNothing()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "Test User",
        });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "No Sharing", AllowSharing = false });
        db.UserGroups.Add(new UserGroupEntity { UserId = UserId.From(userId), GroupId = groupId });
        await db.SaveChangesAsync();

        var message = new PlayPersisted(Guid.NewGuid(), userId, PlayChangeType.Updated);
        var result = await SharePlayHandler.Handle(message, db);

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ChangeType_PassedThrough()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var playId = PlayId.From(Guid.NewGuid());
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "Test User",
        });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Sharing Group", AllowSharing = true });
        db.UserGroups.Add(new UserGroupEntity { UserId = UserId.From(userId), GroupId = groupId });
        db.Plays.Add(CreatePlay(playId, UserId.From(userId)));
        await db.SaveChangesAsync();

        var message = new PlayPersisted(playId.Value, userId, PlayChangeType.Updated);
        var result = await SharePlayHandler.Handle(message, db);

        var shareMsg = result.OfType<SharePlayToGroup>().Single();
        await Assert.That(shareMsg.ChangeType).IsEqualTo(PlayChangeType.Updated);
    }

    [Test]
    public async Task Snapshot_ContainsPlayDetails()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var playId = PlayId.From(Guid.NewGuid());
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "Test User",
        });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Sharing Group", AllowSharing = true });
        db.UserGroups.Add(new UserGroupEntity { UserId = UserId.From(userId), GroupId = groupId });
        db.Plays.Add(CreatePlay(playId, UserId.From(userId)));
        await db.SaveChangesAsync();

        var message = new PlayPersisted(playId.Value, userId, PlayChangeType.Created);
        var result = await SharePlayHandler.Handle(message, db);

        var snapshot = result.OfType<SharePlayToGroup>().Single().Snapshot;
        await Assert.That(snapshot.GameName).IsEqualTo("Wingspan");
        await Assert.That(snapshot.DatePlayed).IsEqualTo(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        await Assert.That(snapshot.Duration).IsEqualTo(TimeSpan.FromMinutes(90));
        await Assert.That(snapshot.LocationName).IsEqualTo("Home");
        await Assert.That(snapshot.Rounds).IsEqualTo(4);
        await Assert.That(snapshot.Comments).IsEqualTo("Great game!");
        await Assert.That(snapshot.GameThumbnailUrl).IsEqualTo("https://example.com/wingspan.jpg");
        await Assert.That(snapshot.BggGameId).IsEqualTo(266192);

        var alice = snapshot.Players.Single(p => p.PlayerName == "Alice");
        await Assert.That(alice.Score).IsEqualTo("85");
        await Assert.That(alice.CalculatedScore).IsEqualTo(85.0);
        await Assert.That(alice.Winner).IsTrue();
        await Assert.That(alice.Rank).IsEqualTo(1);
    }

    [Test]
    public async Task Snapshot_IncludesBgStatsPlayerUuid()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var playId = PlayId.From(Guid.NewGuid());
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "Test User",
        });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Sharing Group", AllowSharing = true });
        db.UserGroups.Add(new UserGroupEntity { UserId = UserId.From(userId), GroupId = groupId });
        var play = CreatePlay(playId, UserId.From(userId));
        db.Plays.Add(play);
        await db.SaveChangesAsync();

        var message = new PlayPersisted(playId.Value, userId, PlayChangeType.Created);
        var result = await SharePlayHandler.Handle(message, db);

        var snapshot = result.OfType<SharePlayToGroup>().Single().Snapshot;
        var aliceEntity = play.Players.Single(p => p.PlayerName == "Alice");
        var aliceSnapshot = snapshot.Players.Single(p => p.PlayerName == "Alice");
        await Assert.That(aliceSnapshot.BgStatsPlayerUuid).IsEqualTo(aliceEntity.BgStatsPlayerUuid);
    }
}
