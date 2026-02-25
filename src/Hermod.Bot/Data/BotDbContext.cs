using Microsoft.EntityFrameworkCore;

namespace Hermod.Bot.Data;

public class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public DbSet<GuildMappingEntity> GuildMappings => Set<GuildMappingEntity>();
    public DbSet<PlayPostEntity> PlayPosts => Set<PlayPostEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bot");

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

        modelBuilder.Entity<PlayPostEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DiscordChannelId)
                .HasColumnType("numeric(20,0)");

            entity.Property(e => e.DiscordMessageId)
                .HasColumnType("numeric(20,0)");

            entity.HasIndex(e => new { e.GroupId, e.PlayId })
                .IsUnique();
        });
    }
}
