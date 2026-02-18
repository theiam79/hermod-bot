using System.Text;
using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public static class UploadPlays
{
    [WolverinePost("/api/plays/upload")]
    public static async Task<(IResult, OutgoingMessages)> Post(
        IFormFile file,
        [FromQuery] Guid? groupId,
        [FromQuery] string? senderDiscordId,
        HermodContext db)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        var result = PlayFileParser.Parse(Encoding.UTF8.GetString(fileBytes));

        var upload = new UploadEntity
        {
            Id = UploadId.From(Guid.NewGuid()),
            FileBytes = fileBytes,
            FileName = file.FileName,
            CreatedAt = DateTime.UtcNow,
        };
        db.Uploads.Add(upload);

        var messages = new OutgoingMessages();
        messages.Add(new PlayFileUploaded(
            upload.Id.Value, groupId, senderDiscordId, result.MePlayerUuid));

        return (Results.Accepted(), messages);
    }
}
