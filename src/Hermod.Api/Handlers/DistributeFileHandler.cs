using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Api.Handlers;

public static class DistributeFileHandler
{
    public static async Task<OutgoingMessages> Handle(PlayFileUploaded message, HermodContext db)
    {
        var upload = await db.Uploads
            .FirstOrDefaultAsync(u => u.Id == UploadId.From(message.UploadId));

        if (upload is null) return [];

        var result = PlayFileParser.Parse(upload.FileContent);

        // Collect all unique player UUIDs across all plays
        var allPlayerUuids = result.Plays
            .SelectMany(p => p.Scores)
            .Select(s => s.Player.Uuid.ToString())
            .Distinct()
            .ToList();

        // Exclude the uploader's player UUID
        var meUuidStr = message.MePlayerUuid?.ToString();

        var messages = new OutgoingMessages();
        foreach (var uuid in allPlayerUuids)
        {
            if (uuid == meUuidStr) continue;
            messages.Add(new DistributePlayFile(message.UploadId, uuid));
        }

        return messages;
    }
}
