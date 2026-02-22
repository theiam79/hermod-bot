using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Api.Handlers;

public static class ExtractPlaysHandler
{
    public static async Task<OutgoingMessages> Handle(PlayFileUploaded message, HermodContext db)
    {
        var upload = await db.Uploads
            .FirstOrDefaultAsync(u => u.Id == UploadId.From(message.UploadId));

        if (upload is null) return [];

        var result = PlayFileParser.Parse(upload.FileContent);

        var messages = new OutgoingMessages();
        foreach (var play in result.Plays)
        {
            messages.Add(new PlayExtracted(
                play, message.GroupId,
                message.MePlayerUuid, message.UploadId,
                message.UploadedById));
        }

        return messages;
    }
}
