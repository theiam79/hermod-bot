namespace Hermod.Data.Entities;

public class UserProfileEntity
{
    public UserId Id { get; set; }
    public required string DisplayName { get; set; }
    public int? BggId { get; set; }
    public string? BggUsername { get; set; }
    public bool SubscribeToPlays { get; set; } = true;
    public bool PostingEnabled { get; set; } = true;
    public bool DistributionEnabled { get; set; } = true;

    public List<PlayerMappingEntity> PlayerMappings { get; set; } = [];
}
