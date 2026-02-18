using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Data;

public class HermodContext(DbContextOptions<HermodContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<GroupEntity> Groups => Set<GroupEntity>();
    public DbSet<UserGroupEntity> UserGroups => Set<UserGroupEntity>();
    public DbSet<PlayEntity> Plays => Set<PlayEntity>();
    public DbSet<PlayPlayerEntity> PlayPlayers => Set<PlayPlayerEntity>();
    public DbSet<PlayerMappingEntity> PlayerMappings => Set<PlayerMappingEntity>();
    public DbSet<UserExternalLoginEntity> UserExternalLogins => Set<UserExternalLoginEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HermodContext).Assembly);
    }
}
