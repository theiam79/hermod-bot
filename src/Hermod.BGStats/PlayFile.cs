namespace Hermod.BGStats;

/// <summary>
/// Raw deserialization model for a .bgsplay JSON file.
/// </summary>
public sealed class PlayFile
{
    public string About { get; init; } = "";
    public List<PlayerSection> Players { get; init; } = [];
    public List<LocationSection> Locations { get; init; } = [];
    public List<GameSection> Games { get; init; } = [];
    public List<PlaySection> Plays { get; init; } = [];
    public UserInfoSection UserInfo { get; init; } = new();

    public sealed class UserInfoSection
    {
        public int MeRefId { get; init; }
    }

    public sealed class PlayerSection
    {
        public int Id { get; init; }
        public Guid Uuid { get; init; }
        public string Name { get; init; } = "";
        public bool IsAnonymous { get; init; }
        public DateTime ModificationDate { get; init; }
    }

    public sealed class LocationSection
    {
        public Guid Uuid { get; init; }
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public DateTime ModificationDate { get; init; }
    }

    public sealed class GameSection
    {
        public Guid Uuid { get; init; }
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public DateTime ModificationDate { get; init; }
        public bool Cooperative { get; init; }
        public bool HighestWins { get; init; }
        public bool NoPoints { get; init; }
        public bool UsesTeams { get; init; }
        public string UrlThumb { get; init; } = "";
        public string UrlImage { get; init; } = "";
        public string BggName { get; init; } = "";
        public int BggYear { get; init; }
        public int BggId { get; init; }
        public string Designers { get; init; } = "";
        public bool IsBaseGame { get; init; }
        public bool IsExpansion { get; init; }
        public int Rating { get; init; }
        public int MinPlayerCount { get; init; }
        public int MaxPlayerCount { get; init; }
        public int MinPlayTime { get; init; }
        public int MaxPlayTime { get; init; }
        public int MinAge { get; init; }
    }

    public sealed class PlaySection
    {
        public Guid Uuid { get; init; }
        public DateTime ModificationDate { get; init; }
        public DateTime EntryDate { get; init; }
        public DateTime PlayDate { get; init; }
        public bool UsesTeams { get; init; }
        public int DurationMin { get; init; }
        public bool Ignored { get; init; }
        public bool ManualWinner { get; init; }
        public int Rounds { get; init; }
        public int LocationRefId { get; init; }
        public int GameRefId { get; init; }
        public string Board { get; init; } = "";
        public string? Comments { get; init; }
        public int ScoringSetting { get; init; }
        public string? Scoresheet { get; init; }
        public string? MetaData { get; init; }
        public List<PlayerScoreSection> PlayerScores { get; init; } = [];
        public List<ExpansionPlaySection> ExpansionPlays { get; init; } = [];
    }

    public sealed class PlayerScoreSection
    {
        public string? Score { get; init; }
        public bool Winner { get; init; }
        public bool NewPlayer { get; init; }
        public bool StartPlayer { get; init; }
        public int PlayerRefId { get; init; }
        public string Role { get; init; } = "";
        public string TeamRole { get; init; } = "";
        public int Rank { get; init; }
        public int SeatOrder { get; init; }
        public string StartPosition { get; init; } = "";
        public string? Team { get; init; }
        public string? MetaData { get; init; }
    }

    public sealed class ExpansionPlaySection
    {
        public int GameRefId { get; init; }
    }
}
