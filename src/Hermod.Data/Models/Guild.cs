namespace Hermod.Data.Models
{
    public class Guild
    {
        public Guid Id { get; init; }
        public ulong GuildId { get; init; }
        public ulong? ManagementRole { get; set; }
        public bool AllowSharing { get; set; }
        public ulong? PostChannelId { get; set; }
        public ICollection<User> Users { get; init; } = new List<User>();
        public List<UserGuild> UserGuilds { get; init; } = new();
    }
}
