namespace Hermod.Data.Entities;

public class PlayPlayerEntity
{
    public PlayPlayerId Id { get; set; }
    public PlayId PlayId { get; set; }
    public required string BgStatsPlayerUuid { get; set; }
    public required string PlayerName { get; set; }
    public UserId? MappedUserId { get; set; }
    public string? Score { get; set; }
    public double? CalculatedScore { get; set; }
    public bool Winner { get; set; }
    public int? Rank { get; set; }
    public string? Role { get; set; }
    public string? Team { get; set; }
    public bool NewPlayer { get; set; }
    public bool StartPlayer { get; set; }

    public PlayEntity Play { get; set; } = null!;
    public UserEntity? MappedUser { get; set; }
}
