namespace Hermod.Data.Models
{
    public class User
    {
        public Guid Id { get; init; }
        public ulong DiscordId { get; init; }
        public int BggId { get; init; }
        public string BggUsername { get; init; } = "";
        public string NormalizedBggUsername { get; init; } = "";
        public ICollection<Guild> Guilds { get; init; } = new List<Guild>();
        public List<UserGuild> UserGuilds { get; init; } = new();
    }
}
