using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroupEntity>
{
    public void Configure(EntityTypeBuilder<UserGroupEntity> builder)
    {
        builder.HasKey(ug => new { ug.UserId, ug.GroupId });

        builder.Property(ug => ug.UserId)
            .HasConversion(id => id.Value, v => UserId.From(v));
        builder.Property(ug => ug.GroupId)
            .HasConversion(id => id.Value, v => GroupId.From(v));

        builder.Property(ug => ug.Role)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(ug => ug.User)
            .WithMany(u => u.UserGroups)
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ug => ug.Group)
            .WithMany(g => g.UserGroups)
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
