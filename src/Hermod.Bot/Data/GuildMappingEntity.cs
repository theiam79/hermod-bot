namespace Hermod.Bot.Data;

public class GuildMappingEntity
{
    public Guid Id { get; set; }
    public ulong DiscordGuildId { get; set; }
    public Guid GroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public ulong? PostChannelId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}
