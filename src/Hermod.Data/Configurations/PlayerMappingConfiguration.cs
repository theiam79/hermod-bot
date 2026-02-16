using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class PlayerMappingConfiguration : IEntityTypeConfiguration<PlayerMappingEntity>
{
    public void Configure(EntityTypeBuilder<PlayerMappingEntity> builder)
    {
        builder.HasKey(pm => pm.Id);
        builder.Property(pm => pm.Id)
            .HasConversion(id => id.Value, v => PlayerMappingId.From(v));

        builder.Property(pm => pm.OwnerUserId)
            .HasConversion(id => id.Value, v => UserId.From(v));
        builder.Property(pm => pm.MappedUserId)
            .HasConversion(id => id.Value, v => UserId.From(v));

        builder.Property(pm => pm.BgStatsPlayerUuid).IsRequired().HasMaxLength(100);

        builder.HasIndex(pm => new { pm.OwnerUserId, pm.BgStatsPlayerUuid }).IsUnique();

        builder.HasOne(pm => pm.Owner)
            .WithMany(u => u.PlayerMappings)
            .HasForeignKey(pm => pm.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.MappedUser)
            .WithMany()
            .HasForeignKey(pm => pm.MappedUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
