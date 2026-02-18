using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class UploadConfiguration : IEntityTypeConfiguration<UploadEntity>
{
    public void Configure(EntityTypeBuilder<UploadEntity> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, v => UploadId.From(v));

        builder.Property(u => u.UploadedById)
            .HasConversion(id => (Guid?)id!.Value.Value, v => v.HasValue ? UserId.From(v.Value) : (UserId?)null);

        builder.Property(u => u.FileBytes).IsRequired();
        builder.Property(u => u.FileName).IsRequired().HasMaxLength(255);

        builder.HasOne(u => u.UploadedBy)
            .WithMany()
            .HasForeignKey(u => u.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
