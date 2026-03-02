namespace Hermod.Messages;

public record EnrollInGroup(string Provider, string ProviderKey, Guid GroupId);

public record EnrollmentResult(EnrollmentStatus Status, Guid? UserId = null);

public enum EnrollmentStatus
{
    Enrolled,
    AlreadyMember,
    NotRegistered,
    GroupNotFound,
}
