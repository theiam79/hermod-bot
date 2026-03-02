using Hermod.Auth;
using Hermod.Api.Features.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class EnrollInGroupHandlerTests
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

    private static async Task<(HermodContext db, AuthDbContext authDb, IServiceProvider services, Guid groupId, Guid userId)> SetupRegisteredUser()
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

        return (db, authDb, BuildServices(authDb), groupId, userId);
    }

    [Test]
    public async Task Handle_RegisteredUser_ReturnsEnrolled()
    {
        var (db, _, services, groupId, _) = await SetupRegisteredUser();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", groupId), db, services);

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.Enrolled);
    }

    [Test]
    public async Task Handle_RegisteredUser_CreatesUserGroupEntity()
    {
        var (db, _, services, groupId, userId) = await SetupRegisteredUser();

        await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", groupId), db, services);
        await db.SaveChangesAsync();

        var membership = await db.UserGroups.SingleAsync();
        await Assert.That(membership.UserId).IsEqualTo(UserId.From(userId));
        await Assert.That(membership.GroupId).IsEqualTo(GroupId.From(groupId));
    }

    [Test]
    public async Task Handle_EnrolledUser_GetsMemberRole()
    {
        var (db, _, services, groupId, _) = await SetupRegisteredUser();

        await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", groupId), db, services);
        await db.SaveChangesAsync();

        var membership = await db.UserGroups.SingleAsync();
        await Assert.That(membership.Role).IsEqualTo(GroupRole.Member);
    }

    [Test]
    public async Task Handle_AlreadyMember_ReturnsAlreadyMember()
    {
        var (db, _, services, groupId, userId) = await SetupRegisteredUser();

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = UserId.From(userId),
            GroupId = GroupId.From(groupId),
            Role = GroupRole.Member,
        });
        await db.SaveChangesAsync();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", groupId), db, services);

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.AlreadyMember);
    }

    [Test]
    public async Task Handle_AlreadyMember_NoDuplicateRow()
    {
        var (db, _, services, groupId, userId) = await SetupRegisteredUser();

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = UserId.From(userId),
            GroupId = GroupId.From(groupId),
            Role = GroupRole.Member,
        });
        await db.SaveChangesAsync();

        await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", groupId), db, services);

        await Assert.That(db.UserGroups.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_DiscordIdNotFound_ReturnsNotRegistered()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var groupId = Guid.NewGuid();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Test Guild" });
        await db.SaveChangesAsync();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "unknown-discord-id", groupId), db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.NotRegistered);
    }

    [Test]
    public async Task Handle_GroupNotFound_ReturnsGroupNotFound()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", Guid.NewGuid()), db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.GroupNotFound);
    }

    [Test]
    public async Task Handle_Enrolled_ResultIncludesUserId()
    {
        var (db, _, services, groupId, userId) = await SetupRegisteredUser();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", groupId), db, services);

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.Enrolled);
        await Assert.That(result.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task Handle_AlreadyMember_ResultIncludesUserId()
    {
        var (db, _, services, groupId, userId) = await SetupRegisteredUser();

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = UserId.From(userId),
            GroupId = GroupId.From(groupId),
            Role = GroupRole.Member,
        });
        await db.SaveChangesAsync();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "123456789", groupId), db, services);

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.AlreadyMember);
        await Assert.That(result.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task Handle_NotRegistered_UserIdIsNull()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var groupId = Guid.NewGuid();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Test Guild" });
        await db.SaveChangesAsync();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "unknown-discord-id", groupId), db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.NotRegistered);
        await Assert.That(result.UserId).IsNull();
    }

    [Test]
    public async Task Handle_RegisteredUser_CreatesProfileIfMissing()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var groupId = Guid.NewGuid();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Test Guild" });
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = "NewUser",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        authDb.ExternalLogins.Add(new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Discord",
            ProviderKey = "999888777",
            CreatedAt = DateTime.UtcNow,
        });
        await authDb.SaveChangesAsync();

        var result = await EnrollInGroupHandler.Handle(
            new EnrollInGroup("Discord", "999888777", groupId), db, BuildServices(authDb));
        await db.SaveChangesAsync();

        await Assert.That(result.Status).IsEqualTo(EnrollmentStatus.Enrolled);

        var profile = await db.UserProfiles.SingleAsync(p => p.Id == UserId.From(userId));
        await Assert.That(profile.DisplayName).IsEqualTo("NewUser");
    }

    [Test]
    public async Task Handle_MultipleUsersCanEnrollInSameGroup()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var groupId = Guid.NewGuid();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Test Guild" });

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(user1Id), DisplayName = "User1" });
        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(user2Id), DisplayName = "User2" });
        await db.SaveChangesAsync();

        authDb.Users.Add(new AuthUser { Id = user1Id, DisplayName = "User1", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        authDb.ExternalLogins.Add(new ExternalLogin { Id = Guid.NewGuid(), UserId = user1Id, Provider = "Discord", ProviderKey = "111", CreatedAt = DateTime.UtcNow });
        authDb.Users.Add(new AuthUser { Id = user2Id, DisplayName = "User2", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        authDb.ExternalLogins.Add(new ExternalLogin { Id = Guid.NewGuid(), UserId = user2Id, Provider = "Discord", ProviderKey = "222", CreatedAt = DateTime.UtcNow });
        await authDb.SaveChangesAsync();

        var services = BuildServices(authDb);
        var result1 = await EnrollInGroupHandler.Handle(new EnrollInGroup("Discord", "111", groupId), db, services);
        await db.SaveChangesAsync();
        var result2 = await EnrollInGroupHandler.Handle(new EnrollInGroup("Discord", "222", groupId), db, services);
        await db.SaveChangesAsync();

        await Assert.That(result1.Status).IsEqualTo(EnrollmentStatus.Enrolled);
        await Assert.That(result2.Status).IsEqualTo(EnrollmentStatus.Enrolled);
        await Assert.That(db.UserGroups.Count()).IsEqualTo(2);
    }
}
