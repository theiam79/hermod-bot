using Microsoft.EntityFrameworkCore;

namespace Hermod.Bot.Data;

public class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public DbSet<GuildMappingEntity> GuildMappings => Set<GuildMappingEntity>();
    public DbSet<PlayPostEntity> PlayPosts => Set<PlayPostEntity>();
    public DbSet<DiscordUserMappingEntity> DiscordUserMappings => Set<DiscordUserMappingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuildMappingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DiscordGuildId)
                .HasColumnType("numeric(20,0)");

            entity.HasIndex(e => e.DiscordGuildId)
                .IsUnique();

            entity.Property(e => e.PostChannelId)
                .HasColumnType("numeric(20,0)");
        });

        modelBuilder.Entity<DiscordUserMappingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DiscordUserId)
                .HasColumnType("numeric(20,0)");

            entity.HasIndex(e => e.DiscordUserId)
                .IsUnique();

            entity.HasIndex(e => e.HermodUserId)
                .IsUnique();
        });

        modelBuilder.Entity<PlayPostEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DiscordChannelId)
                .HasColumnType("numeric(20,0)");

            entity.Property(e => e.DiscordMessageId)
                .HasColumnType("numeric(20,0)");

            entity.HasIndex(e => new { e.GroupId, e.PlayId })
                .IsUnique();

            entity.Property(e => e.PlayersJson)
                .HasColumnType("jsonb");
        });
    }
}
