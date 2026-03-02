using Hermod.Api.Handlers;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class RegisterCommunityHandlerTests
{
    private static HermodContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    [Test]
    public async Task Handle_CreatesGroupEntity()
    {
        await using var db = CreateInMemoryDb();

        var message = new RegisterCommunity("Discord", "123456789", "Test Server");
        await RegisterCommunityHandler.Handle(message, db);
        await db.SaveChangesAsync();

        await Assert.That(db.Groups.Count()).IsEqualTo(1);
        var group = await db.Groups.SingleAsync();
        await Assert.That(group.Name).IsEqualTo("Test Server");
    }

    [Test]
    public async Task Handle_SetsAllowSharingFalse()
    {
        await using var db = CreateInMemoryDb();

        var message = new RegisterCommunity("Discord", "123456789", "Test Server");
        await RegisterCommunityHandler.Handle(message, db);
        await db.SaveChangesAsync();

        var group = await db.Groups.SingleAsync();
        await Assert.That(group.AllowSharing).IsFalse();
    }

    [Test]
    public async Task Handle_ReturnsDeterministicGroupId()
    {
        await using var db = CreateInMemoryDb();

        var message = new RegisterCommunity("Discord", "123456789", "Test Server");
        var result = await RegisterCommunityHandler.Handle(message, db);
        await db.SaveChangesAsync();

        var expectedId = GroupIdFactory.ForCommunity("Discord", "123456789");
        await Assert.That(result.GroupId).IsEqualTo(expectedId);
    }

    [Test]
    public async Task Handle_PassesThroughProviderAndPlatformId()
    {
        await using var db = CreateInMemoryDb();

        var message = new RegisterCommunity("Discord", "987654321", "Another Server");
        var result = await RegisterCommunityHandler.Handle(message, db);
        await db.SaveChangesAsync();

        await Assert.That(result.Provider).IsEqualTo("Discord");
        await Assert.That(result.PlatformId).IsEqualTo("987654321");
    }

    [Test]
    public async Task Handle_SameCommunityTwice_UpsertsOneGroup()
    {
        await using var db = CreateInMemoryDb();

        await RegisterCommunityHandler.Handle(new RegisterCommunity("Discord", "123456789", "Old Name"), db);
        await db.SaveChangesAsync();
        await RegisterCommunityHandler.Handle(new RegisterCommunity("Discord", "123456789", "New Name"), db);
        await db.SaveChangesAsync();

        await Assert.That(db.Groups.Count()).IsEqualTo(1);
        var group = await db.Groups.SingleAsync();
        await Assert.That(group.Name).IsEqualTo("New Name");
    }

    [Test]
    public async Task Handle_SameCommunityTwice_ReturnsSameGroupId()
    {
        await using var db = CreateInMemoryDb();

        var first = await RegisterCommunityHandler.Handle(new RegisterCommunity("Discord", "123456789", "Server"), db);
        await db.SaveChangesAsync();
        var second = await RegisterCommunityHandler.Handle(new RegisterCommunity("Discord", "123456789", "Server"), db);
        await db.SaveChangesAsync();

        await Assert.That(first.GroupId).IsEqualTo(second.GroupId);
    }

    [Test]
    public async Task Handle_ExistingGroupWithSharingDisabled_PreservesAllowSharing()
    {
        await using var db = CreateInMemoryDb();
        var groupId = GroupId.From(GroupIdFactory.ForCommunity("Discord", "123456789"));
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Server", AllowSharing = false });
        await db.SaveChangesAsync();

        await RegisterCommunityHandler.Handle(new RegisterCommunity("Discord", "123456789", "Server"), db);
        await db.SaveChangesAsync();

        var group = await db.Groups.SingleAsync();
        await Assert.That(group.AllowSharing).IsFalse();
    }

    [Test]
    public async Task Handle_DifferentCommunities_CreateDifferentGroups()
    {
        await using var db = CreateInMemoryDb();

        var first = await RegisterCommunityHandler.Handle(new RegisterCommunity("Discord", "111", "Server A"), db);
        await db.SaveChangesAsync();
        var second = await RegisterCommunityHandler.Handle(new RegisterCommunity("Discord", "222", "Server B"), db);
        await db.SaveChangesAsync();

        await Assert.That(db.Groups.Count()).IsEqualTo(2);
        await Assert.That(first.GroupId).IsNotEqualTo(second.GroupId);
    }
}
