using Hermod.Api.Mappers;
using Hermod.Contracts.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Groups;

public static class GetGroup
{
    public static async Task<GroupEntity?> LoadAsync(Guid id, HermodContext db)
        => await db.Groups.FirstOrDefaultAsync(g => g.Id == GroupId.From(id));

    [Authorize]
    [WolverineGet("/api/groups/{id}")]
    public static GroupResponse Get(GroupEntity group)
        => ResponseMapper.ToResponse(group);
}
