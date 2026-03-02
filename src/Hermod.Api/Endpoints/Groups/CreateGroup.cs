using System.Security.Claims;
using Hermod.Data;
using Hermod.Data.Entities;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Groups;

public static class CreateGroup
{
    public static IResult? Before(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue("hermod:user_id");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _))
            return Results.Unauthorized();

        return WolverineContinue.Result();
    }

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
}
