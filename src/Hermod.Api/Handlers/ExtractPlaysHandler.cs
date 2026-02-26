using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hermod.Api.Handlers;

public static class ExtractPlaysHandler
{
    public static async Task<OutgoingMessages> Handle(PlayFileUploaded message, HermodContext db, ILogger logger)
    {
        var upload = await db.Uploads.FindAsync(UploadId.From(message.UploadId))
            ?? throw new InvalidOperationException($"Upload {message.UploadId} not found");

        logger.LogInformation("Parsing upload {UploadId}: FileContent length = {Length}",
            message.UploadId, upload.FileContent.Length);

        var result = PlayFileParser.Parse(upload.FileContent);

        logger.LogInformation("Parsed {PlayCount} play(s) from upload {UploadId}",
            result.Plays.Count, message.UploadId);

        return [..result.Plays.ConvertAll(play => new PlayExtracted(
            play, message.MePlayerUuid, message.UploadId,
            message.UploadedById))];
    }
}
