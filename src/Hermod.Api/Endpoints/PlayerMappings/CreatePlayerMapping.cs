using Hermod.Contracts.PlayerMappings;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.PlayerMappings;

public static class CreatePlayerMapping
{
    [Authorize]
    [EmptyResponse]
    [WolverinePost("/api/player-mappings")]
    public static async Task Post(CreatePlayerMappingRequest request, HermodContext db)
    {
        var ownerId = UserId.From(request.OwnerUserId);
        var mappedUserId = UserId.From(request.MappedUserId);

        var existing = await db.PlayerMappings.FirstOrDefaultAsync(
            pm => pm.OwnerUserId == ownerId && pm.BgStatsPlayerUuid == request.BgStatsPlayerUuid);

        if (existing is not null)
        {
            existing.MappedUserId = mappedUserId;
        }
        else
        {
            db.PlayerMappings.Add(new PlayerMappingEntity
            {
                Id = PlayerMappingId.From(Guid.NewGuid()),
                OwnerUserId = ownerId,
                BgStatsPlayerUuid = request.BgStatsPlayerUuid,
                MappedUserId = mappedUserId,
            });
        }
    }
}
