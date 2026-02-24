namespace Hermod.Messages;

public record EnrollInGroup(string DiscordId, Guid GroupId);

public record EnrollmentResult(EnrollmentStatus Status);

public enum EnrollmentStatus
{
    Enrolled,
    AlreadyMember,
    NotRegistered,
    GroupNotFound,
}
