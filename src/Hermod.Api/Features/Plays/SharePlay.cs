using Hermod.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Api.Features.Plays;

public static class SharePlayHandler
{
    public static async Task<OutgoingMessages> Handle(PlayPersisted message, HermodContext db)
    {
        var uploadedById = UserId.From(message.UploadedById);

        var groupIds = await db.UserGroups
            .Where(ug => ug.UserId == uploadedById && ug.Group.AllowSharing)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        if (groupIds.Count == 0)
            return [];

        var profile = await db.UserProfiles.FindAsync(uploadedById);
        if (profile is { PostingEnabled: false })
            return [];

        var play = await db.Plays
            .Include(p => p.Players)
            .FirstOrDefaultAsync(p => p.Id == PlayId.From(message.PlayId));

        if (play is null)
            throw new InvalidOperationException($"Play {message.PlayId} not found — may not be committed yet.");

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

        return [..groupIds.ConvertAll(gid => new SharePlayToGroup(message.PlayId, gid.Value, message.ChangeType, snapshot))];
    }
}
