using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data.Entities;
using Wolverine;
using Wolverine.Persistence;

namespace Hermod.Api.Handlers;

public static class ExtractPlaysHandler
{
    public static OutgoingMessages Handle(PlayFileUploaded message, [Entity] UploadEntity upload)
    {
        if (upload is null) return [];

        var result = PlayFileParser.Parse(upload.FileContent);

        return [..result.Plays.ConvertAll(play => new PlayExtracted(
            play, message.MePlayerUuid, message.UploadId,
            message.UploadedById))];
    }
}
