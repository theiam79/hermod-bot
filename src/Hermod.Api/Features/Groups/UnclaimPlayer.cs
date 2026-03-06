using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Data;
using Hermod.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Features.Groups;

public static class UnclaimPlayerHandler
{
    public static async Task<(UnclaimPlayerResult, ClaimChanged?)> Handle(
        UnclaimPlayer message, HermodContext db)
    {
        var userId = UserId.From(message.UserId);

        var mapping = await db.PlayerMappings
            .FirstOrDefaultAsync(pm => pm.BgStatsPlayerUuid == message.BgStatsPlayerUuid);

        if (mapping is null)
            return (new UnclaimPlayerResult(UnclaimStatus.NotFound), null);

        if (mapping.MappedUserId != userId)
            return (new UnclaimPlayerResult(UnclaimStatus.Forbidden), null);

        var linkedPlayers = await db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid
                      && pp.MappedUserId == userId)
            .ToListAsync();
        foreach (var p in linkedPlayers)
            p.MappedUserId = null;

        db.PlayerMappings.Remove(mapping);

        return (
            new UnclaimPlayerResult(UnclaimStatus.Removed),
            new ClaimChanged(message.BgStatsPlayerUuid, null));
    }
}

public static class UnclaimPlayerEndpoint
{
    [Authorize]
    [WolverineDelete("/api/players/claims/{bgStatsPlayerUuid}")]
    public static async Task<(IResult, ClaimChanged?)> Delete(
        string bgStatsPlayerUuid,
        ClaimsPrincipal user,
        HermodContext db)
    {
        var userId = user.GetUserId()!.Value;
        var (result, claimChanged) = await UnclaimPlayerHandler.Handle(
            new UnclaimPlayer(userId, bgStatsPlayerUuid), db);

        var httpResult = result.Status switch
        {
            UnclaimStatus.Removed => Results.NoContent(),
            UnclaimStatus.NotFound => Results.NotFound(),
            UnclaimStatus.Forbidden => Results.Forbid(),
            _ => Results.Problem()
        };

        return (httpResult, claimChanged);
    }
}
