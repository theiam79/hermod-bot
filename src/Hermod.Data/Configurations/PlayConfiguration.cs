using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class PlayConfiguration : IEntityTypeConfiguration<PlayEntity>
{
    public void Configure(EntityTypeBuilder<PlayEntity> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, v => PlayId.From(v));

        builder.Property(p => p.UploadedById)
            .HasConversion(id => id.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(p => p.BgStatsPlayUuid).IsRequired().HasMaxLength(100);
        builder.Property(p => p.GameName).IsRequired().HasMaxLength(500);
        builder.Property(p => p.GameThumbnailUrl).HasMaxLength(1000);
        builder.Property(p => p.LocationName).HasMaxLength(500);
        builder.Property(p => p.ImageUrl).HasMaxLength(1000);

        builder.HasIndex(p => new { p.BgStatsPlayUuid, p.UploadedById }).IsUnique();
        builder.HasIndex(p => p.DatePlayed);
        builder.HasIndex(p => p.UploadedById);

        builder.Property(p => p.UploadId)
            .HasConversion(id => id.HasValue ? (Guid?)id.Value.Value : null, v => v.HasValue ? UploadId.From(v.Value) : (UploadId?)null);

        builder.HasOne(p => p.Upload)
            .WithMany(u => u.Plays)
            .HasForeignKey(p => p.UploadId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
