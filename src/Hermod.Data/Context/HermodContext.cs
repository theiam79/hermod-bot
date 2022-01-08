using Hermod.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Data.Context
{
    public class HermodContext : DbContext
    {
        public HermodContext(DbContextOptions<HermodContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Guild> Guilds => Set<Guild>();
        public DbSet<UserGuild> UserGuilds => Set<UserGuild>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<User>()
                .HasMany(u => u.Guilds)
                .WithMany(g => g.Users)
                .UsingEntity<UserGuild>(
                    j => j
                        .HasOne(ug => ug.Guild)
                        .WithMany(g => g.UserGuilds)
                        .HasForeignKey(ug => ug.GuildId),
                    j => j
                        .HasOne(ug => ug.User)
                        .WithMany(u => u.UserGuilds)
                        .HasForeignKey(ug => ug.UserId),
                    j => j
                        .HasKey(ug => new { ug.UserId, ug.Guild })
                    );
        }
        
    }
}
