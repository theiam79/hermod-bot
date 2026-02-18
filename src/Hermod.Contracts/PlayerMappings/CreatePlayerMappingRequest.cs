namespace Hermod.Contracts.PlayerMappings;

public sealed record CreatePlayerMappingRequest
{
    public required Guid OwnerUserId { get; init; }
    public required string BgStatsPlayerUuid { get; init; }
    public required Guid MappedUserId { get; init; }
}
