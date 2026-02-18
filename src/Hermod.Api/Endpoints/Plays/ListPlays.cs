using Hermod.Api.Mappers;
using Hermod.Contracts.Plays;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public static class ListPlays
{
    [Authorize]
    [WolverineGet("/api/plays")]
    public static async Task<PlayResponse[]> Get(
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? userId,
        HermodContext db)
    {
        IQueryable<PlayEntity> query = db.Plays.Include(p => p.Players);

        if (groupId.HasValue)
            query = query.Where(p => p.GroupId == GroupId.From(groupId.Value));
        else if (userId.HasValue)
            query = query.Where(p => p.UploadedById == UserId.From(userId.Value));
        else
            return [];

        var entities = await query
            .OrderByDescending(p => p.DatePlayed)
            .ToListAsync();

        return ResponseMapper.ToResponseArray(entities);
    }
}
