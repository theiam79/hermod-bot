using Hermod.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Features.Groups;

public static class GetUserClaimsHandler
{
    public static async Task<GetUserClaimsResult> Handle(GetUserClaims message, HermodContext db)
    {
        var userId = UserId.From(message.UserId);

        var mappings = await db.PlayerMappings
            .Where(pm => pm.MappedUserId == userId)
            .Select(pm => pm.BgStatsPlayerUuid)
            .ToListAsync();

        if (mappings.Count == 0)
            return new GetUserClaimsResult([]);

        var claims = new List<UserClaimInfo>();

        foreach (var uuid in mappings)
        {
            var playerName = await db.PlayPlayers
                .Where(pp => pp.BgStatsPlayerUuid == uuid)
                .Select(pp => pp.PlayerName)
                .FirstOrDefaultAsync();

            var playCount = await db.PlayPlayers
                .CountAsync(pp => pp.BgStatsPlayerUuid == uuid);

            if (playerName is not null)
                claims.Add(new UserClaimInfo(uuid, playerName, playCount));
        }

        return new GetUserClaimsResult(claims);
    }
}
