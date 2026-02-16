using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class PlayPlayerConfiguration : IEntityTypeConfiguration<PlayPlayerEntity>
{
    public void Configure(EntityTypeBuilder<PlayPlayerEntity> builder)
    {
        builder.HasKey(pp => pp.Id);
        builder.Property(pp => pp.Id)
            .HasConversion(id => id.Value, v => PlayPlayerId.From(v));

        builder.Property(pp => pp.PlayId)
            .HasConversion(id => id.Value, v => PlayId.From(v));
        builder.Property(pp => pp.MappedUserId)
            .HasConversion(id => (Guid?)id!.Value.Value, v => v.HasValue ? UserId.From(v.Value) : (UserId?)null);

        builder.Property(pp => pp.BgStatsPlayerUuid).IsRequired().HasMaxLength(100);
        builder.Property(pp => pp.PlayerName).IsRequired().HasMaxLength(200);
        builder.Property(pp => pp.Score).HasMaxLength(200);
        builder.Property(pp => pp.Role).HasMaxLength(500);
        builder.Property(pp => pp.Team).HasMaxLength(100);

        builder.HasOne(pp => pp.Play)
            .WithMany(p => p.Players)
            .HasForeignKey(pp => pp.PlayId);

        builder.HasOne(pp => pp.MappedUser)
            .WithMany()
            .HasForeignKey(pp => pp.MappedUserId)
            .IsRequired(false);
    }
}
