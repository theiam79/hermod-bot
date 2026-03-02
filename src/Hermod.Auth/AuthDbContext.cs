using Microsoft.EntityFrameworkCore;

namespace Hermod.Auth;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> Users => Set<AuthUser>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
