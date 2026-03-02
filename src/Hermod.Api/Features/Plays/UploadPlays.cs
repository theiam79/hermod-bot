using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.BGStats;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;

namespace Hermod.Api.Features.Plays;

public record UploadResult(Guid UploadId, int PlayCount);

public record PlayFileUploaded(Guid UploadId, Guid? MePlayerUuid, Guid UploadedById);

[Authorize]
public static class UploadPlays
{
    private const int MaxFileSizeBytes = 1_048_576; // 1 MB

    [WolverinePost("/api/plays/upload")]
    public static async Task<(IResult, OutgoingMessages)> Post(IFormFile file, ClaimsPrincipal user, [FromServices] HermodContext db)
    {
        if (file.Length > MaxFileSizeBytes)
            return (Results.BadRequest("File exceeds maximum size of 1 MB."), []);

        PlayFileResult parsed;
        string content;
        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            content = await reader.ReadToEndAsync();
            parsed = PlayFileParser.Parse(content);
        }
        catch (Exception) when (file.Length <= MaxFileSizeBytes)
        {
            return (Results.BadRequest("Invalid .bgsplay file content."), []);
        }

        var userId = user.GetUserId()!.Value;
        var uploadId = UploadId.From(Guid.NewGuid());

        db.Uploads.Add(new UploadEntity
        {
            Id = uploadId,
            UploadedById = UserId.From(userId),
            FileContent = content,
            FileName = file.FileName,
            CreatedAt = DateTime.UtcNow,
        });

        var messages = new OutgoingMessages
        {
            new PlayFileUploaded(uploadId.Value, parsed.MePlayerUuid, userId),
        };

        return (Results.Accepted(null, new UploadResult(uploadId.Value, parsed.Plays.Count)), messages);
    }
}
