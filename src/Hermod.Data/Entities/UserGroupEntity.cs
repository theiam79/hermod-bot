namespace Hermod.Data.Entities;

public class UserGroupEntity
{
    public UserId UserId { get; set; }
    public GroupId GroupId { get; set; }
    public GroupRole Role { get; set; } = GroupRole.Member;

    public UserEntity User { get; set; } = null!;
    public GroupEntity Group { get; set; } = null!;
}

public enum GroupRole
{
    Member,
    Admin,
}
