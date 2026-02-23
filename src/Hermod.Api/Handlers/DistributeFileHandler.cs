using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data.Entities;
using Wolverine;
using Wolverine.Persistence;

namespace Hermod.Api.Handlers;

public static class DistributeFileHandler
{
    public static OutgoingMessages Handle(PlayFileUploaded message, [Entity] UploadEntity upload)
    {
        if (upload is null) return [];

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
