using Hermod.Data;

namespace Hermod.Core.Models;

public sealed record UserProfile
{
    public required UserId Id { get; init; }
    public required string DisplayName { get; init; }
    public ulong? DiscordId { get; init; }
    public int? BggId { get; init; }
    public string? BggUsername { get; init; }
    public bool SubscribeToPlays { get; init; }
}
