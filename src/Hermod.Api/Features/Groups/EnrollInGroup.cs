using Hermod.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Features.Groups;

public static class EnrollInGroupHandler
{
    // IExternalUserResolver resolved via IServiceProvider so Wolverine sees only HermodContext
    // for transaction management (AuthDbContext is a transitive dependency of ExternalUserResolver).
    public static async Task<EnrollmentResult> Handle(
        EnrollInGroup message, HermodContext db, IServiceProvider services)
    {
        var groupId = GroupId.From(message.GroupId);
        var group = await db.Groups.FindAsync(groupId);
        if (group is null)
            return new EnrollmentResult(EnrollmentStatus.GroupNotFound);

        var userResolver = services.GetRequiredService<IExternalUserResolver>();
        var externalUser = await userResolver.ResolveAsync(message.Provider, message.ProviderKey);
        if (externalUser is null)
            return new EnrollmentResult(EnrollmentStatus.NotRegistered);

        var userId = UserId.From(externalUser.UserId);

        var hasProfile = await db.UserProfiles.AnyAsync(p => p.Id == userId);
        if (!hasProfile)
        {
            db.UserProfiles.Add(new UserProfileEntity
            {
                Id = userId,
                DisplayName = externalUser.DisplayName,
            });
        }

        var existing = await db.UserGroups
            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
        if (existing)
            return new EnrollmentResult(EnrollmentStatus.AlreadyMember, externalUser.UserId);

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = userId,
            GroupId = groupId,
            Role = GroupRole.Member,
        });
        return new EnrollmentResult(EnrollmentStatus.Enrolled, externalUser.UserId);
    }
}
