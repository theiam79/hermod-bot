using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hermod.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HermodContext>
{
    public HermodContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HermodContext>();
        var connectionString = Environment.GetEnvironmentVariable("HERMOD_CONNECTION_STRING")
            ?? "Host=localhost;Database=hermod;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new HermodContext(optionsBuilder.Options);
    }
}
