namespace Hermod.Contracts.Plays;

public sealed record PlayResponse
{
    public required Guid Id { get; init; }
    public required string GameName { get; init; }
    public int? BggGameId { get; init; }
    public string? GameThumbnailUrl { get; init; }
    public DateTime DatePlayed { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? LocationName { get; init; }
    public int? Rounds { get; init; }
    public string? Comments { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<PlayPlayerResponse> Players { get; init; } = [];
}

public sealed record PlayPlayerResponse
{
    public required string PlayerName { get; init; }
    public string? BgStatsPlayerUuid { get; init; }
    public Guid? MappedUserId { get; init; }
    public string? Score { get; init; }
    public double? CalculatedScore { get; init; }
    public bool Winner { get; init; }
    public int? Rank { get; init; }
    public string? Role { get; init; }
    public string? Team { get; init; }
}
