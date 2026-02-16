namespace Hermod.BGStats.Models;

public record Game
{
    public required Guid Uuid { get; init; }
    public required string Name { get; init; }
    public DateTime ModificationDate { get; init; }
    public bool Cooperative { get; init; }
    public bool HighestScoreWins { get; init; }
    public bool NoPoints { get; init; }
    public bool UsesTeams { get; init; }
    public string ThumbnailUrl { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public string BggName { get; init; } = "";
    public int BggYear { get; init; }
    public int BggId { get; init; }
    public string Designers { get; init; } = "";
    public bool IsBaseGame { get; init; }
    public bool IsExpansion { get; init; }
    public int Rating { get; init; }
    public int MinPlayerCount { get; init; }
    public int MaxPlayerCount { get; init; }
    public TimeSpan MinPlayTime { get; init; }
    public TimeSpan MaxPlayTime { get; init; }
    public int MinAge { get; init; }
    public List<Game> Expansions { get; init; } = [];
}
