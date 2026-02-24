using Microsoft.EntityFrameworkCore;

namespace Hermod.Bot.Data;

public class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public DbSet<GuildMappingEntity> GuildMappings => Set<GuildMappingEntity>();

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
        });
    }
}
