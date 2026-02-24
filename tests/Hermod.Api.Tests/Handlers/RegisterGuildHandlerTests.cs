using Hermod.Api.Handlers;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class RegisterGuildHandlerTests
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

        var message = new RegisterGuild(123456789UL, "Test Server");
        await RegisterGuildHandler.Handle(message, db);

        await Assert.That(db.Groups.Count()).IsEqualTo(1);
        var group = await db.Groups.SingleAsync();
        await Assert.That(group.Name).IsEqualTo("Test Server");
    }

    [Test]
    public async Task Handle_SetsAllowSharingTrue()
    {
        await using var db = CreateInMemoryDb();

        var message = new RegisterGuild(123456789UL, "Test Server");
        await RegisterGuildHandler.Handle(message, db);

        var group = await db.Groups.SingleAsync();
        await Assert.That(group.AllowSharing).IsTrue();
    }

    [Test]
    public async Task Handle_ReturnsDeterministicGroupId()
    {
        await using var db = CreateInMemoryDb();

        var message = new RegisterGuild(123456789UL, "Test Server");
        var result = await RegisterGuildHandler.Handle(message, db);

        var expectedId = GroupIdFactory.ForDiscordGuild(123456789UL);
        await Assert.That(result.GroupId).IsEqualTo(expectedId);
    }

    [Test]
    public async Task Handle_PassesThroughDiscordGuildId()
    {
        await using var db = CreateInMemoryDb();

        var message = new RegisterGuild(987654321UL, "Another Server");
        var result = await RegisterGuildHandler.Handle(message, db);

        await Assert.That(result.DiscordGuildId).IsEqualTo(987654321UL);
    }

    [Test]
    public async Task Handle_SameGuildTwice_UpsertsOneGroup()
    {
        await using var db = CreateInMemoryDb();

        await RegisterGuildHandler.Handle(new RegisterGuild(123456789UL, "Old Name"), db);
        await RegisterGuildHandler.Handle(new RegisterGuild(123456789UL, "New Name"), db);

        await Assert.That(db.Groups.Count()).IsEqualTo(1);
        var group = await db.Groups.SingleAsync();
        await Assert.That(group.Name).IsEqualTo("New Name");
    }

    [Test]
    public async Task Handle_SameGuildTwice_ReturnsSameGroupId()
    {
        await using var db = CreateInMemoryDb();

        var first = await RegisterGuildHandler.Handle(new RegisterGuild(123456789UL, "Server"), db);
        var second = await RegisterGuildHandler.Handle(new RegisterGuild(123456789UL, "Server"), db);

        await Assert.That(first.GroupId).IsEqualTo(second.GroupId);
    }

    [Test]
    public async Task Handle_ExistingGroupWithSharingDisabled_ReEnablesSharing()
    {
        await using var db = CreateInMemoryDb();
        var groupId = GroupId.From(GroupIdFactory.ForDiscordGuild(123456789UL));
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Server", AllowSharing = false });
        await db.SaveChangesAsync();

        await RegisterGuildHandler.Handle(new RegisterGuild(123456789UL, "Server"), db);

        var group = await db.Groups.SingleAsync();
        await Assert.That(group.AllowSharing).IsTrue();
    }

    [Test]
    public async Task Handle_DifferentGuilds_CreateDifferentGroups()
    {
        await using var db = CreateInMemoryDb();

        var first = await RegisterGuildHandler.Handle(new RegisterGuild(111UL, "Server A"), db);
        var second = await RegisterGuildHandler.Handle(new RegisterGuild(222UL, "Server B"), db);

        await Assert.That(db.Groups.Count()).IsEqualTo(2);
        await Assert.That(first.GroupId).IsNotEqualTo(second.GroupId);
    }
}
