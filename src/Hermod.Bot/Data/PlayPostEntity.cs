namespace Hermod.Bot.Data;

public class PlayPostEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid PlayId { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ulong DiscordMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
