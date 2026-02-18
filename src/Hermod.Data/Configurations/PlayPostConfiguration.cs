using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class PlayPostConfiguration : IEntityTypeConfiguration<PlayPostEntity>
{
    public void Configure(EntityTypeBuilder<PlayPostEntity> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, v => PlayPostId.From(v));

        builder.Property(p => p.PlayId)
            .HasConversion(id => id.Value, v => PlayId.From(v));

        builder.HasOne(p => p.Play)
            .WithMany(p => p.Posts)
            .HasForeignKey(p => p.PlayId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enforce one post per guild per play
        builder.HasIndex(p => new { p.PlayId, p.DiscordGuildId }).IsUnique();
    }
}
