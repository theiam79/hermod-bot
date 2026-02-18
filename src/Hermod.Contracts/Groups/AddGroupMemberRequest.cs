namespace Hermod.Contracts.Groups;

public sealed record AddGroupMemberRequest
{
    public required Guid UserId { get; init; }
    public string Role { get; init; } = "Member";
}
