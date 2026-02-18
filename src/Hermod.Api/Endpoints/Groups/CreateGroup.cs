using Hermod.Api.Mappers;
using Hermod.Contracts.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Groups;

public static class CreateGroup
{
    [Authorize]
    [WolverinePost("/api/groups")]
    public static async Task<IResult> Post(CreateGroupRequest request, HermodContext db)
    {
        var existing = await db.Groups.FirstOrDefaultAsync(g => g.DiscordGuildId == request.DiscordGuildId);
        if (existing is not null)
            return Results.Ok(ResponseMapper.ToResponse(existing));

        var group = new GroupEntity
        {
            Id = GroupId.From(Guid.NewGuid()),
            Name = request.Name,
            DiscordGuildId = request.DiscordGuildId,
            DiscordPostChannelId = request.DefaultChannelId,
        };

        db.Groups.Add(group);
        var response = ResponseMapper.ToResponse(group);
        return Results.Created($"/api/groups/{response.Id}", response);
    }
}
