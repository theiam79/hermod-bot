namespace Hermod.Contracts.Groups;

public sealed record CreateGroupRequest
{
    public required ulong DiscordGuildId { get; init; }
    public required string Name { get; init; }
    public ulong? DefaultChannelId { get; init; }
}
