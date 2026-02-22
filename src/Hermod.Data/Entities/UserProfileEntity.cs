namespace Hermod.Data.Entities;

public class UserProfileEntity
{
    public UserId Id { get; set; }
    public required string DisplayName { get; set; }
    public int? BggId { get; set; }
    public string? BggUsername { get; set; }
    public bool SubscribeToPlays { get; set; } = true;

    public List<UserGroupEntity> UserGroups { get; set; } = [];
    public List<PlayEntity> UploadedPlays { get; set; } = [];
    public List<PlayerMappingEntity> PlayerMappings { get; set; } = [];
}
