namespace Hermod.Contracts.Groups;

public sealed record GroupResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public ulong? DiscordGuildId { get; init; }
    public ulong? DiscordPostChannelId { get; init; }
    public bool AllowSharing { get; init; }
}
