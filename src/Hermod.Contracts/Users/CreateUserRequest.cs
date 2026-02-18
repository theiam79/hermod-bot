namespace Hermod.Contracts.Users;

public sealed record CreateUserRequest
{
    public required ulong DiscordId { get; init; }
    public required string DisplayName { get; init; }
}
