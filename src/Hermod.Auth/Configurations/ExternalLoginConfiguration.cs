using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Auth.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ProviderKey).IsRequired().HasMaxLength(200);

        builder.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
