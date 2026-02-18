using Hermod.Data;

namespace Hermod.Api.Handlers;

public record UploadPlaysCommand(
    Stream FileStream,
    UserId UploadedById,
    GroupId? GroupId,
    string? ImageUrl);
