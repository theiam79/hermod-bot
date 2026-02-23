using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Hermod.Data.Entities;
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

    public static OutgoingMessages Handle(PlayFileUploaded message, UploadEntity upload)
    {
        var result = PlayFileParser.Parse(upload.FileContent);

        return [..result.Plays.ConvertAll(play => new PlayExtracted(
            play, message.MePlayerUuid, message.UploadId,
            message.UploadedById))];
    }
}
