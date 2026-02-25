using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hermod.Api.Handlers;

public static class ExtractPlaysHandler
{
    public static async Task<(HandlerContinuation, UploadEntity?)> LoadAsync(
        PlayFileUploaded message, HermodContext db)
    {
        var upload = await db.Uploads.FindAsync(UploadId.From(message.UploadId));
        if (upload is null) return (HandlerContinuation.Stop, null);
        return (HandlerContinuation.Continue, upload);
    }

    public static OutgoingMessages Handle(PlayFileUploaded message, UploadEntity upload, ILogger logger)
    {
        logger.LogInformation("Parsing upload {UploadId}: FileContent length = {Length}",
            message.UploadId, upload.FileContent?.Length ?? -1);

        var result = PlayFileParser.Parse(upload.FileContent);

        logger.LogInformation("Parsed {PlayCount} play(s) from upload {UploadId}",
            result.Plays.Count, message.UploadId);

        return [..result.Plays.ConvertAll(play => new PlayExtracted(
            play, message.MePlayerUuid, message.UploadId,
            message.UploadedById))];
    }
}
