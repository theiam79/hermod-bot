using Hermod.Api.Handlers;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class UpdateGroupSharingHandlerTests
{
    private static HermodContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    [Test]
    public async Task Handle_DisablesSharing()
    {
        await using var db = CreateInMemoryDb();
        var groupId = GroupId.From(Guid.NewGuid());
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Test Group", AllowSharing = true });
        await db.SaveChangesAsync();

        await UpdateGroupSharingHandler.Handle(new UpdateGroupSharing(groupId.Value, false), db);
        await db.SaveChangesAsync();

        var group = await db.Groups.SingleAsync();
        await Assert.That(group.AllowSharing).IsFalse();
    }

    [Test]
    public async Task Handle_EnablesSharing()
    {
        await using var db = CreateInMemoryDb();
        var groupId = GroupId.From(Guid.NewGuid());
        db.Groups.Add(new GroupEntity { Id = groupId, Name = "Test Group", AllowSharing = false });
        await db.SaveChangesAsync();

        await UpdateGroupSharingHandler.Handle(new UpdateGroupSharing(groupId.Value, true), db);
        await db.SaveChangesAsync();

        var group = await db.Groups.SingleAsync();
        await Assert.That(group.AllowSharing).IsTrue();
    }

    [Test]
    public async Task Handle_NonExistentGroup_NoOp()
    {
        await using var db = CreateInMemoryDb();

        await UpdateGroupSharingHandler.Handle(new UpdateGroupSharing(Guid.NewGuid(), false), db);

        await Assert.That(db.Groups.Count()).IsEqualTo(0);
    }
}
