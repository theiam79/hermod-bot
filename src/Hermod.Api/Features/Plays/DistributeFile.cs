using Hermod.BGStats;
using Hermod.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Api.Features.Plays;

public static class DistributeFileHandler
{
    public static async Task<OutgoingMessages> Handle(
        PlayFileUploaded message, HermodContext db)
    {
        var upload = await db.Uploads.FindAsync(UploadId.From(message.UploadId))
            ?? throw new InvalidOperationException($"Upload {message.UploadId} not found");

        var uploaderProfile = await db.UserProfiles.FindAsync(UserId.From(message.UploadedById));
        if (uploaderProfile is { DistributionEnabled: false })
            return [];

        var result = PlayFileParser.Parse(upload.FileContent);

        // Collect all unique player UUIDs across all plays, excluding the uploader
        var allPlayerUuids = result.Plays
            .SelectMany(p => p.Scores)
            .Select(s => s.Player.Uuid.ToString())
            .Distinct()
            .Except([message.MePlayerUuid?.ToString()])
            .ToList();

        if (allPlayerUuids.Count == 0)
            return [];

        // Find registered users mapped to these player UUIDs
        var mappedUserIds = await db.PlayerMappings
            .Where(pm => allPlayerUuids.Contains(pm.BgStatsPlayerUuid))
            .Select(pm => pm.MappedUserId)
            .Distinct()
            .ToListAsync();

        if (mappedUserIds.Count == 0)
            return [];

        // Filter to subscribed users, excluding the uploader
        var uploadedById = UserId.From(message.UploadedById);
        var subscribedUserIds = await db.UserProfiles
            .Where(u => mappedUserIds.Contains(u.Id) && u.SubscribeToPlays && u.Id != uploadedById)
            .Select(u => u.Id.Value)
            .ToListAsync();

        if (subscribedUserIds.Count == 0)
            return [];

        return [..subscribedUserIds.ConvertAll(uid =>
            new DistributePlayFile(upload.FileContent, upload.FileName, uid))];
    }
}
