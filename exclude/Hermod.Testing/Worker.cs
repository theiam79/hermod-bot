using Hermod.Data.Context;
using Hermod.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Testing
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var _hermodContext = scope.ServiceProvider.GetRequiredService<HermodContext>();

            var userQuery = _hermodContext
                .Users

                //.Include(u => u.Guilds)
                //.Include(u => u.UserGuilds)
                .Select(x => x.BggUsername)
                .AsNoTracking();

            var queryString = userQuery.ToQueryString();

            var users = await userQuery.ToListAsync();

            User newUserr = new()
            {
                BggId = 1,
                BggUsername = "test",
                DiscordId = 1,
                NormalizedBggUsername = "TEST",
                SubscribeToPlays = true,
            };

            _hermodContext.Users.Add(newUserr);

            await _hermodContext.SaveChangesAsync();


            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}