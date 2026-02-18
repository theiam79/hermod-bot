using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<GroupEntity>
{
    public void Configure(EntityTypeBuilder<GroupEntity> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasConversion(id => id.Value, v => GroupId.From(v));

        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);

        builder.Property(g => g.SpamThreshold).HasDefaultValue(3);

        builder.HasIndex(g => g.DiscordGuildId).IsUnique().HasFilter("DiscordGuildId IS NOT NULL");
    }
}
