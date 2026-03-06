using Hermod.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Api.Features.Plays;

public static class RefreshClaimEmbedsHandler
{
    public static async Task<OutgoingMessages> Handle(ClaimChanged message, HermodContext db)
    {
        // Find all plays containing the affected player UUID
        var plays = await db.Plays
            .Include(p => p.Players)
            .Where(p => p.Players.Any(pp => pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid))
            .ToListAsync();

        if (plays.Count == 0)
            return [];

        // For each play, find all groups the uploader shares to
        var uploaderIds = plays.Select(p => p.UploadedById).Distinct().ToList();

        var uploaderGroups = await db.UserGroups
            .Where(ug => uploaderIds.Contains(ug.UserId) && ug.Group.AllowSharing)
            .Select(ug => new { ug.UserId, ug.GroupId })
            .ToListAsync();

        var uploaderGroupMap = uploaderGroups
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GroupId).ToList());

        var messages = new OutgoingMessages();

        foreach (var play in plays)
        {
            if (!uploaderGroupMap.TryGetValue(play.UploadedById, out var groupIds))
                continue;

            var snapshot = new PlaySnapshot(
                play.GameName,
                play.DatePlayed,
                play.Duration,
                play.LocationName,
                play.Rounds,
                play.Comments,
                play.GameThumbnailUrl,
                play.BggGameId,
                play.Players.Select(p => new PlayerSnapshot(
                    p.BgStatsPlayerUuid,
                    p.PlayerName,
                    p.Score,
                    p.CalculatedScore,
                    p.Winner,
                    p.Rank,
                    p.Role,
                    p.Team,
                    p.MappedUserId?.Value)).ToList());

            foreach (var gid in groupIds)
                messages.Add(new SharePlayToGroup(play.Id.Value, gid.Value, PlayChangeType.Updated, snapshot));
        }

        return messages;
    }
}
