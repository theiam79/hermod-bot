using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Http;

namespace Hermod.Api.Features.Groups;

public static class UnclaimPlayerHandler
{
    public static async Task<UnclaimPlayerResult> Handle(
        UnclaimPlayer message, HermodContext db)
    {
        var userId = UserId.From(message.UserId);

        var mapping = await db.PlayerMappings
            .FirstOrDefaultAsync(pm => pm.BgStatsPlayerUuid == message.BgStatsPlayerUuid);

        if (mapping is null)
            return new UnclaimPlayerResult(UnclaimStatus.NotFound);

        if (mapping.MappedUserId != userId)
            return new UnclaimPlayerResult(UnclaimStatus.Forbidden);

        var affectedPlayIds = await db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid
                      && pp.MappedUserId == userId)
            .Select(pp => pp.PlayId.Value)
            .Distinct()
            .ToListAsync();

        var linkedPlayers = await db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid
                      && pp.MappedUserId == userId)
            .ToListAsync();
        foreach (var p in linkedPlayers)
            p.MappedUserId = null;

        db.PlayerMappings.Remove(mapping);

        return new UnclaimPlayerResult(
            UnclaimStatus.Removed,
            new ClaimRemoved(message.BgStatsPlayerUuid, affectedPlayIds));
    }
}

public static class UnclaimPlayerEndpoint
{
    [Authorize]
    [WolverineDelete("/api/players/claims/{bgStatsPlayerUuid}")]
    public static async Task<IResult> Delete(
        string bgStatsPlayerUuid,
        ClaimsPrincipal user,
        HermodContext db,
        IMessageBus bus)
    {
        var userId = user.GetUserId()!.Value;
        var result = await UnclaimPlayerHandler.Handle(
            new UnclaimPlayer(userId, bgStatsPlayerUuid), db);

        if (result.Event is not null)
            await bus.PublishAsync(result.Event);

        return result.Status switch
        {
            UnclaimStatus.Removed => Results.NoContent(),
            UnclaimStatus.NotFound => Results.NotFound(),
            UnclaimStatus.Forbidden => Results.Forbid(),
            _ => Results.Problem()
        };
    }
}
