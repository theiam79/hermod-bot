namespace Hermod.Data.Entities;

public class PlayerMappingEntity
{
    public PlayerMappingId Id { get; set; }
    public required string BgStatsPlayerUuid { get; set; }
    public UserId MappedUserId { get; set; }

    public UserProfileEntity MappedUser { get; set; } = null!;
}
