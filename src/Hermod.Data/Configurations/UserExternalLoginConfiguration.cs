using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLoginEntity>
{
    public void Configure(EntityTypeBuilder<UserExternalLoginEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => ExternalLoginId.From(v));

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, v => UserId.From(v));

        builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ProviderKey).IsRequired().HasMaxLength(200);

        builder.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany(u => u.ExternalLogins)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
