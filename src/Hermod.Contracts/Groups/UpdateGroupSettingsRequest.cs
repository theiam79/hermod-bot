namespace Hermod.Contracts.Groups;

public sealed record UpdateGroupSettingsRequest
{
    public bool AllowSharing { get; init; }
    public ulong? PostChannelId { get; init; }
}
