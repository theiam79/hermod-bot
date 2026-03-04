using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Data;

public class HermodContext(DbContextOptions<HermodContext> options) : DbContext(options)
{
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();
    public DbSet<GroupEntity> Groups => Set<GroupEntity>();
    public DbSet<UserGroupEntity> UserGroups => Set<UserGroupEntity>();
    public DbSet<PlayEntity> Plays => Set<PlayEntity>();
    public DbSet<PlayPlayerEntity> PlayPlayers => Set<PlayPlayerEntity>();
    public DbSet<PlayerMappingEntity> PlayerMappings => Set<PlayerMappingEntity>();
    public DbSet<UploadEntity> Uploads => Set<UploadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HermodContext).Assembly);
    }
}
