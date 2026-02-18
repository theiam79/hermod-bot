namespace Hermod.Data.Entities;

public class PlayPostEntity
{
    public PlayPostId Id { get; set; }
    public PlayId PlayId { get; set; }
    public ulong DiscordGuildId { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ulong DiscordMessageId { get; set; }
    public DateTime PostedAt { get; set; }

    public PlayEntity Play { get; set; } = null!;
}
