namespace Hermod.Contracts.Plays;

public sealed record UploadPlaysRequest
{
    public required Guid UploadedById { get; init; }
    public Guid? GroupId { get; init; }
    public string? ImageUrl { get; init; }
}
