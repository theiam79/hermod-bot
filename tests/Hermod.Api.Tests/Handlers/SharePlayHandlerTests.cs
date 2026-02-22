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

    [Test]
    public async Task UserInTwoGroups_OneSharingEnabled_EmitsOneMessage()
    {
        await using var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
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
        await db.SaveChangesAsync();

        var message = new PlayPersisted(Guid.NewGuid(), userId, PlayChangeType.Created);
        var result = await SharePlayHandler.Handle(message, db);

        await Assert.That(result).Count().IsEqualTo(1);

        var shareMsg = result.OfType<SharePlayToGroup>().Single();
        await Assert.That(shareMsg.GroupId).IsEqualTo(sharingGroupId.Value);
        await Assert.That(shareMsg.PlayId).IsEqualTo(message.PlayId);
        await Assert.That(shareMsg.ChangeType).IsEqualTo(PlayChangeType.Created);
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
        var groupId = GroupId.From(Guid.NewGuid());

        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "Test User",
        });
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Sharing Group", AllowSharing = true });
        db.UserGroups.Add(new UserGroupEntity { UserId = UserId.From(userId), GroupId = groupId });
        await db.SaveChangesAsync();

        var message = new PlayPersisted(Guid.NewGuid(), userId, PlayChangeType.Updated);
        var result = await SharePlayHandler.Handle(message, db);

        var shareMsg = result.OfType<SharePlayToGroup>().Single();
        await Assert.That(shareMsg.ChangeType).IsEqualTo(PlayChangeType.Updated);
    }
}
