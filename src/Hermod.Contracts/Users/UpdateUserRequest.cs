namespace Hermod.Contracts.Users;

public sealed record UpdateUserRequest
{
    public bool SubscribeToPlays { get; init; }
    public int? BggId { get; init; }
    public string? BggUsername { get; init; }
}
