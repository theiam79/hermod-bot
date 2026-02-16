using Hermod.Data;

namespace Hermod.Core.Models;

public sealed record GroupSummary
{
    public required GroupId Id { get; init; }
    public required string Name { get; init; }
    public ulong? DiscordGuildId { get; init; }
    public ulong? DiscordPostChannelId { get; init; }
    public bool AllowSharing { get; init; }
}
