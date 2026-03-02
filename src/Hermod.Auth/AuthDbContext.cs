using Microsoft.EntityFrameworkCore;

namespace Hermod.Auth;

public class AuthUser
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

public class ExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Provider { get; set; }
    public required string ProviderKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public AuthUser User { get; set; } = null!;
}

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> Users => Set<AuthUser>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthUser>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(u => u.AvatarUrl).HasMaxLength(1000);
        });

        modelBuilder.Entity<ExternalLogin>(builder =>
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            builder.Property(e => e.ProviderKey).IsRequired().HasMaxLength(200);

            builder.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
