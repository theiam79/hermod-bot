using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hermod.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HermodContext>
{
    public HermodContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HermodContext>();
        optionsBuilder.UseSqlite("Data Source=hermod.db");
        return new HermodContext(optionsBuilder.Options);
    }
}
