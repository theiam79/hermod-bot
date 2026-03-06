using Hermod.Auth;
using Hermod.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Features.Groups;

public static class LeaveGroupHandler
{
    // IExternalUserResolver resolved via IServiceProvider so Wolverine sees only HermodContext
    // for transaction management (AuthDbContext is a transitive dependency of ExternalUserResolver).
    public static async Task<LeaveGroupResult> Handle(
        LeaveGroup message, HermodContext db, IServiceProvider services)
    {
        var groupId = GroupId.From(message.GroupId);
        var group = await db.Groups.FindAsync(groupId);
        if (group is null)
            return new LeaveGroupResult(LeaveGroupStatus.GroupNotFound);

        var userResolver = services.GetRequiredService<IExternalUserResolver>();
        var externalUser = await userResolver.ResolveAsync(message.Provider, message.ProviderKey);
        if (externalUser is null)
            return new LeaveGroupResult(LeaveGroupStatus.NotRegistered);

        var userId = UserId.From(externalUser.UserId);

        var membership = await db.UserGroups
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
        if (membership is null)
            return new LeaveGroupResult(LeaveGroupStatus.NotMember, externalUser.UserId);

        db.UserGroups.Remove(membership);
        return new LeaveGroupResult(LeaveGroupStatus.Left, externalUser.UserId);
    }
}
