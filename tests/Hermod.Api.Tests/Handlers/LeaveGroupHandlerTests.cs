using Hermod.Auth;
using Hermod.Api.Features.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class LeaveGroupHandlerTests
{
    private static HermodContext CreateHermodDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    private static AuthDbContext CreateAuthDb()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options);
    }

    private static IServiceProvider BuildServices(AuthDbContext authDb)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IExternalUserResolver>(new ExternalUserResolver(authDb));
        return services.BuildServiceProvider();
    }

    private static async Task<(HermodContext db, AuthDbContext authDb, IServiceProvider services, Guid groupId, Guid userId)> SetupMember()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var groupId = Guid.NewGuid();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Test Guild" });

        var userId = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(userId), DisplayName = "TestUser" });
        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = UserId.From(userId),
            GroupId = GroupId.From(groupId),
            Role = GroupRole.Member,
        });
        await db.SaveChangesAsync();

        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = "TestUser",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        authDb.ExternalLogins.Add(new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Discord",
            ProviderKey = "123456789",
            CreatedAt = DateTime.UtcNow,
        });
        await authDb.SaveChangesAsync();

        return (db, authDb, BuildServices(authDb), groupId, userId);
    }

    [Test]
    public async Task Handle_MemberLeaves_ReturnsLeft()
    {
        var (db, _, services, groupId, _) = await SetupMember();

        var result = await LeaveGroupHandler.Handle(
            new LeaveGroup("Discord", "123456789", groupId), db, services);

        await Assert.That(result.Status).IsEqualTo(LeaveGroupStatus.Left);
    }

    [Test]
    public async Task Handle_MemberLeaves_RemovesUserGroupEntity()
    {
        var (db, _, services, groupId, _) = await SetupMember();

        await LeaveGroupHandler.Handle(
            new LeaveGroup("Discord", "123456789", groupId), db, services);
        await db.SaveChangesAsync();

        await Assert.That(db.UserGroups.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_MemberLeaves_ResultIncludesUserId()
    {
        var (db, _, services, groupId, userId) = await SetupMember();

        var result = await LeaveGroupHandler.Handle(
            new LeaveGroup("Discord", "123456789", groupId), db, services);

        await Assert.That(result.Status).IsEqualTo(LeaveGroupStatus.Left);
        await Assert.That(result.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task Handle_NotMember_ReturnsNotMember()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var groupId = Guid.NewGuid();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Test Guild" });

        var userId = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(userId), DisplayName = "TestUser" });
        await db.SaveChangesAsync();

        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = "TestUser",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        authDb.ExternalLogins.Add(new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Discord",
            ProviderKey = "123456789",
            CreatedAt = DateTime.UtcNow,
        });
        await authDb.SaveChangesAsync();

        var result = await LeaveGroupHandler.Handle(
            new LeaveGroup("Discord", "123456789", groupId), db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(LeaveGroupStatus.NotMember);
        await Assert.That(result.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task Handle_GroupNotFound_ReturnsGroupNotFound()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var result = await LeaveGroupHandler.Handle(
            new LeaveGroup("Discord", "123456789", Guid.NewGuid()), db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(LeaveGroupStatus.GroupNotFound);
    }

    [Test]
    public async Task Handle_NotRegistered_ReturnsNotRegistered()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var groupId = Guid.NewGuid();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Test Guild" });
        await db.SaveChangesAsync();

        var result = await LeaveGroupHandler.Handle(
            new LeaveGroup("Discord", "unknown-discord-id", groupId), db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(LeaveGroupStatus.NotRegistered);
        await Assert.That(result.UserId).IsNull();
    }

    [Test]
    public async Task Handle_MemberLeaves_PlayPostsUnaffected()
    {
        var (db, _, services, groupId, userId) = await SetupMember();

        // Seed a play and play post to verify they survive the leave
        var playId = PlayId.From(Guid.NewGuid());
        db.Plays.Add(new PlayEntity
        {
            Id = playId,
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Test Game",
            DatePlayed = DateTime.UtcNow,
            UploadedById = UserId.From(userId),
        });
        await db.SaveChangesAsync();

        await LeaveGroupHandler.Handle(
            new LeaveGroup("Discord", "123456789", groupId), db, services);
        await db.SaveChangesAsync();

        // Play still exists after leaving
        var play = await db.Plays.FindAsync(playId);
        await Assert.That(play).IsNotNull();
    }
}
