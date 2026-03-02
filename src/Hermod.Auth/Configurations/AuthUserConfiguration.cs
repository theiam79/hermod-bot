using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Auth.Configurations;

public class AuthUserConfiguration : IEntityTypeConfiguration<AuthUser>
{
    public void Configure(EntityTypeBuilder<AuthUser> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.AvatarUrl).HasMaxLength(1000);
    }
}
