using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hermod.Bot.Data;

public class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HERMOD_CONNECTION_STRING")
            ?? "Host=localhost;Database=hermod;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BotDbContext(options);
    }
}
