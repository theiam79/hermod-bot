namespace Hermod.Contracts.PlayerMappings;

public sealed record PlayerMappingResponse
{
    public required string BgStatsPlayerUuid { get; init; }
    public required Guid MappedUserId { get; init; }
}
