using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Groups;

public record CreateGroupRequest(string Name);
public record GroupResponse(Guid Id, string Name, bool AllowSharing);

public static class GroupEndpoints
{
    [WolverinePost("/api/groups")]
    public static async Task<IResult> Create(CreateGroupRequest request, HermodContext db)
    {
        var group = new GroupEntity
        {
            Id = GroupId.From(Guid.NewGuid()),
            Name = request.Name,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        return Results.Created($"/api/groups/{group.Id.Value}",
            new GroupResponse(group.Id.Value, group.Name, group.AllowSharing));
    }

    [WolverineGet("/api/groups/{id:guid}")]
    public static async Task<IResult> Get(Guid id, HermodContext db)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == GroupId.From(id));
        if (group is null)
            return Results.NotFound();

        return Results.Ok(new GroupResponse(group.Id.Value, group.Name, group.AllowSharing));
    }
}
