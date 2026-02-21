namespace Hermod.Data.Entities;

public class GroupEntity
{
    public GroupId Id { get; set; }
    public required string Name { get; set; }
    public bool AllowSharing { get; set; } = false;

    public List<UserGroupEntity> UserGroups { get; set; } = [];
}
