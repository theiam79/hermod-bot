namespace Hermod.Data.Entities;

public class PlayEntity
{
    public PlayId Id { get; set; }
    public UserId? UploadedById { get; set; }
    public GroupId? GroupId { get; set; }
    public required string BgStatsPlayUuid { get; set; }
    public required string GameName { get; set; }
    public int? BggGameId { get; set; }
    public string? GameThumbnailUrl { get; set; }
    public DateTime DatePlayed { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? LocationName { get; set; }
    public int? Rounds { get; set; }
    public string? Comments { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public UploadId? UploadId { get; set; }

    public UserEntity? UploadedBy { get; set; }
    public GroupEntity? Group { get; set; }
    public UploadEntity? Upload { get; set; }
    public List<PlayPlayerEntity> Players { get; set; } = [];
    public List<PlayPostEntity> Posts { get; set; } = [];
}
