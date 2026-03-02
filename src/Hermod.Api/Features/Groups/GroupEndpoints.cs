using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Features.Groups;

public record CreateGroupRequest(string Name);
public record GroupResponse(Guid Id, string Name, bool AllowSharing);

public static class GroupEndpoints
{
    [WolverineGet("/api/groups/{id:guid}")]
    public static async Task<IResult> Get(Guid id, HermodContext db)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == GroupId.From(id));
        if (group is null)
            return Results.NotFound();

        return Results.Ok(new GroupResponse(group.Id.Value, group.Name, group.AllowSharing));
    }

    [Authorize]
    [WolverineGet("/api/groups")]
    public static async Task<IResult> List(ClaimsPrincipal user, HermodContext db)
    {
        var userId = UserId.From(user.GetUserId()!.Value);

        var groups = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => new GroupResponse(ug.Group.Id.Value, ug.Group.Name, ug.Group.AllowSharing))
            .ToListAsync();

        return Results.Ok(groups);
    }

    [Authorize]
    [WolverinePost("/api/groups")]
    public static async Task<IResult> Post(CreateGroupRequest request, HermodContext db)
    {
        var group = new GroupEntity
        {
            Id = GroupId.From(Guid.NewGuid()),
            Name = request.Name,
        };
        db.Groups.Add(group);

        return Results.Created($"/api/groups/{group.Id.Value}",
            new GroupResponse(group.Id.Value, group.Name, group.AllowSharing));
    }

    [Authorize]
    [WolverinePut("/api/groups/{groupId:guid}/membership")]
    public static async Task<IResult> JoinGroup(Guid groupId, ClaimsPrincipal user, [FromServices] HermodContext db)
    {
        var gid = GroupId.From(groupId);
        var group = await db.Groups.FindAsync(gid);
        if (group is null)
            return Results.NotFound();

        var userId = UserId.From(user.GetUserId()!.Value);
        var already = await db.UserGroups
            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == gid);
        if (already)
            return Results.NoContent();

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = userId,
            GroupId = gid,
            Role = GroupRole.Member,
        });

        return Results.Created($"/api/groups/{groupId}/membership", null);
    }
}
