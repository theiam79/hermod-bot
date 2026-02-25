using Hermod.Api.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hermod.Api.Handlers;

public static class EnrollInGroupHandler
{
    // AuthDbContext resolved via IServiceProvider so Wolverine sees only HermodContext
    // for transaction management (AuthDbContext is read-only here).
    public static async Task<EnrollmentResult> Handle(
        EnrollInGroup message, HermodContext db, IServiceProvider services)
    {
        var groupId = GroupId.From(message.GroupId);
        var group = await db.Groups.FindAsync(groupId);
        if (group is null)
            return new EnrollmentResult(EnrollmentStatus.GroupNotFound);

        var authDb = services.GetRequiredService<AuthDbContext>();
        var login = await authDb.ExternalLogins
            .FirstOrDefaultAsync(e => e.Provider == "Discord" && e.ProviderKey == message.DiscordId);
        if (login is null)
            return new EnrollmentResult(EnrollmentStatus.NotRegistered);

        var userId = UserId.From(login.UserId);
        var existing = await db.UserGroups
            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
        if (existing)
            return new EnrollmentResult(EnrollmentStatus.AlreadyMember, login.UserId);

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = userId,
            GroupId = groupId,
            Role = GroupRole.Member,
        });
        await db.SaveChangesAsync();

        return new EnrollmentResult(EnrollmentStatus.Enrolled, login.UserId);
    }
}
