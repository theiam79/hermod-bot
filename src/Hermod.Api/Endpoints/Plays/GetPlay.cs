using Hermod.Api.Mappers;
using Hermod.Contracts.Plays;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public static class GetPlay
{
    public static async Task<PlayEntity?> LoadAsync(Guid id, HermodContext db)
        => await db.Plays
            .Include(p => p.Players)
            .FirstOrDefaultAsync(p => p.Id == PlayId.From(id));

    [Authorize]
    [WolverineGet("/api/plays/{id}")]
    public static PlayResponse Get(PlayEntity play)
        => ResponseMapper.ToResponse(play);
}
