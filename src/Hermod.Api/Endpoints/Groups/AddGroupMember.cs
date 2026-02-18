using Hermod.Contracts.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Groups;

public static class AddGroupMember
{
    [Authorize]
    [EmptyResponse]
    [WolverinePost("/api/groups/{id}/members")]
    public static async Task Post(Guid id, AddGroupMemberRequest request, HermodContext db)
    {
        var groupId = GroupId.From(id);
        var userId = UserId.From(request.UserId);
        var role = Enum.Parse<GroupRole>(request.Role, ignoreCase: true);

        var exists = await db.UserGroups.AnyAsync(
            ug => ug.UserId == userId && ug.GroupId == groupId);

        if (exists) return;

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = userId,
            GroupId = groupId,
            Role = role,
        });
    }
}
