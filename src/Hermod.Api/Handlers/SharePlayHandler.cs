using Hermod.Api.Messages;
using Hermod.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Api.Handlers;

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

        var play = await db.Plays
            .Include(p => p.Players)
            .FirstAsync(p => p.Id == PlayId.From(message.PlayId));

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
