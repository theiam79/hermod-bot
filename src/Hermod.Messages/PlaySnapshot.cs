namespace Hermod.Messages;

public record PlaySnapshot(
    string GameName,
    DateTime DatePlayed,
    TimeSpan? Duration,
    string? LocationName,
    int? Rounds,
    string? Comments,
    string? GameThumbnailUrl,
    int? BggGameId,
    List<PlayerSnapshot> Players);

public record PlayerSnapshot(
    string PlayerName,
    string? Score,
    double? CalculatedScore,
    bool Winner,
    int? Rank,
    string? Role,
    string? Team,
    Guid? MappedUserId);
