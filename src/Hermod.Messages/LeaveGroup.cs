namespace Hermod.Messages;

public record LeaveGroup(string Provider, string ProviderKey, Guid GroupId);

public record LeaveGroupResult(LeaveGroupStatus Status, Guid? UserId = null);

public enum LeaveGroupStatus
{
    Left,
    NotMember,
    NotRegistered,
    GroupNotFound,
}
