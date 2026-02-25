namespace Hermod.Bot.Data;

public class DiscordUserMappingEntity
{
    public Guid Id { get; set; }
    public ulong DiscordUserId { get; set; }
    public Guid HermodUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
