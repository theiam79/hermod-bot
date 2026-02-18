using Hermod.Contracts.PlayerMappings;
using Hermod.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.PlayerMappings;

public static class ListPlayerMappings
{
    [Authorize]
    [WolverineGet("/api/player-mappings")]
    public static async Task<PlayerMappingResponse[]> Get(
        [FromQuery] Guid ownerId,
        HermodContext db)
    {
        return await db.PlayerMappings
            .Where(pm => pm.OwnerUserId == UserId.From(ownerId))
            .Select(pm => new PlayerMappingResponse
            {
                BgStatsPlayerUuid = pm.BgStatsPlayerUuid,
                MappedUserId = pm.MappedUserId.Value,
            })
            .ToArrayAsync();
    }
}
