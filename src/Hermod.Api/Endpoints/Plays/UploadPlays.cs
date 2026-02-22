using System.Security.Claims;
using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Hermod.Data.Entities;
using Wolverine;

namespace Hermod.Api.Endpoints.Plays;

public static class UploadPlays
{
    private const long MaxFileSizeBytes = 1_048_576; // 1 MB

    public static void MapUploadEndpoint(this WebApplication app)
    {
        app.MapPost("/api/plays/upload", HandleUpload)
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleUpload(IFormFile file, ClaimsPrincipal user, IMessageBus bus, HermodContext db)
    {
        var userIdClaim = user.FindFirstValue("hermod:user_id");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        if (file.Length > MaxFileSizeBytes)
            return Results.BadRequest("File exceeds maximum size of 1 MB.");

        using var reader = new StreamReader(file.OpenReadStream());
        var fileContent = await reader.ReadToEndAsync();

        PlayFileResult result;
        try
        {
            result = PlayFileParser.Parse(fileContent);
        }
        catch
        {
            return Results.BadRequest("Invalid .bgsplay file.");
        }

        var uploadId = UploadId.From(Guid.NewGuid());
        var upload = new UploadEntity
        {
            Id = uploadId,
            UploadedById = UserId.From(userId),
            FileContent = fileContent,
            FileName = file.FileName,
            CreatedAt = DateTime.UtcNow,
        };

        db.Uploads.Add(upload);
        await db.SaveChangesAsync();

        await bus.PublishAsync(new PlayFileUploaded(
            uploadId.Value,
            GroupId: null,
            MePlayerUuid: result.MePlayerUuid,
            UploadedById: userId));

        return Results.Accepted(value: new
        {
            UploadId = uploadId.Value,
            PlayCount = result.Plays.Count,
        });
    }
}
