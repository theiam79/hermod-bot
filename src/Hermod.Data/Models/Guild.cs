namespace Hermod.Data.Models
{
    public class Guild
    {
        public Guid Id { get; init; }
        public ulong GuildId { get; init; }
        public ulong PostChannelId { get; set; }
        public ICollection<User> Users { get; init; } = new List<User>();
        public List<UserGuild> UserGuilds { get; init; } = new();
    }
}
