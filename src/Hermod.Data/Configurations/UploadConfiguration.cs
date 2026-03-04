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
            .HasConversion(id => id.Value, v => UserId.From(v));

        builder.Property(u => u.FileContent).IsRequired().HasColumnType("jsonb");
        builder.Property(u => u.FileName).IsRequired().HasMaxLength(255);
    }
}
