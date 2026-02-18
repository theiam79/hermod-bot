namespace Hermod.Contracts.Users;

public sealed record UserResponse
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public ulong? DiscordId { get; init; }
    public int? BggId { get; init; }
    public string? BggUsername { get; init; }
    public bool SubscribeToPlays { get; init; }
}
