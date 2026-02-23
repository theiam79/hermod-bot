using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Hermod.Data.Entities;
using Wolverine;

namespace Hermod.Api.Handlers;

public static class DistributeFileHandler
{
    public static async Task<(HandlerContinuation, UploadEntity?)> LoadAsync(
        PlayFileUploaded message, HermodContext db)
    {
        var upload = await db.Uploads.FindAsync(UploadId.From(message.UploadId));
        if (upload is null) return (HandlerContinuation.Stop, null);
        return (HandlerContinuation.Continue, upload);
    }

    public static OutgoingMessages Handle(PlayFileUploaded message, UploadEntity upload)
    {
        var result = PlayFileParser.Parse(upload.FileContent);

        // Collect all unique player UUIDs across all plays
        var allPlayerUuids = result.Plays
            .SelectMany(p => p.Scores)
            .Select(s => s.Player.Uuid.ToString())
            .Where(uuid => uuid != null)
            .Distinct()
            .Except([message.MePlayerUuid?.ToString()])
            .ToList();

        return [..allPlayerUuids.ConvertAll(uuid => new DistributePlayFile(message.UploadId, uuid!))];
    }
}
