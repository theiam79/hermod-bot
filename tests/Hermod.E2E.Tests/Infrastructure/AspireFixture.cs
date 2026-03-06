using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Hermod.Auth;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TUnit.Core;
using TUnit.Core.Interfaces;
using WireMock.Server;
using WireMock.Settings;

namespace Hermod.E2E.Tests.Infrastructure;

/// <summary>
/// Boots the real Aspire AppHost with PostgreSQL, NATS, API, and Bot (in Testing mode).
/// WireMock simulates the Discord REST API. Shared per test session.
/// </summary>
public class AspireFixture : IAsyncInitializer, IAsyncDisposable
{
    public DistributedApplication App { get; private set; } = null!;
    public WireMockServer DiscordApi { get; private set; } = null!;

    private string _hermodDbConn = null!;
    private string _authDbConn = null!;
    private string _botDbConn = null!;

    public async Task InitializeAsync()
    {
        // 1. Start WireMock on HTTPS before AppHost so the URL is available.
        //    NetCord hardcodes "https://" so WireMock must speak TLS.
        //    Uses the dotnet dev certificate (run `dotnet dev-certs https` if missing).
        DiscordApi = WireMockServer.Start(new WireMockServerSettings
        {
            UseSSL = true,
        });
        WireMockHelper.ConfigureDiscordStubs(DiscordApi);

        // Parse WireMock URL into host:port for NetCord's Hostname config
        var wireMockUrl = DiscordApi.Url ?? throw new InvalidOperationException("WireMock URL is null");
        var wireMockUri = new Uri(wireMockUrl);
        var wireMockHostPort = $"{wireMockUri.Host}:{wireMockUri.Port}";

        // 2. Boot the real AppHost
        // Pass config via args — settings.Configuration does NOT reliably merge into
        // builder.Configuration for Aspire 13.x, but CLI args do.
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hermod_AppHost>(
                [
                    $"--Testing:DiscordApiBaseUrl={wireMockHostPort}",
                ],
                (options, settings) =>
                {
                    var config = settings.Configuration!;
                    // Required parameters (dummy values — bot doesn't connect to real Discord)
                    config["Parameters:discord-token"] = "e2e-test-token";
                    config["Parameters:discord-client-id"] = "e2e-test-client-id";
                    config["Parameters:discord-client-secret"] = "e2e-test-secret";
                });

        builder.Services.ConfigureHttpClientDefaults(cb =>
            cb.AddStandardResilienceHandler());

        App = await builder.BuildAsync();
        await App.StartAsync();

        // 3. Wait for services to be healthy
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("hermod-api", cts.Token);

        // Bot may not have health checks configured — wait for Running state instead
        await App.ResourceNotifications
            .WaitForResourceAsync("hermod-bot", "Running", cts.Token);

        // 4. Extract connection strings for direct DB access in tests
        _hermodDbConn = await App.GetConnectionStringAsync("hermod-db")
            ?? throw new InvalidOperationException("hermod-db connection string not available");
        _authDbConn = await App.GetConnectionStringAsync("auth-db")
            ?? throw new InvalidOperationException("auth-db connection string not available");
        _botDbConn = await App.GetConnectionStringAsync("bot-db")
            ?? throw new InvalidOperationException("bot-db connection string not available");

        // 5. Ensure migrations have been applied before tests seed data.
        //    The API runs migrations on startup but the health check may pass
        //    before migrations complete. MigrateAsync is idempotent — safe even
        //    if the API has already applied them.
        await using (var authDb = CreateAuthDb())
            await authDb.Database.MigrateAsync(cts.Token);
        await using (var hermodDb = CreateHermodDb())
            await hermodDb.Database.MigrateAsync(cts.Token);
    }

    /// <summary>
    /// Creates an HttpClient pointed at the API service.
    /// </summary>
    public HttpClient CreateApiClient()
        => App.CreateHttpClient("hermod-api");

    /// <summary>
    /// Creates a new HermodContext for seeding/asserting against hermod-db.
    /// Caller is responsible for disposing.
    /// </summary>
    public HermodContext CreateHermodDb()
        => new(new DbContextOptionsBuilder<HermodContext>()
            .UseNpgsql(_hermodDbConn).Options);

    /// <summary>
    /// Creates a new AuthDbContext for seeding/asserting against auth-db.
    /// Caller is responsible for disposing.
    /// </summary>
    public AuthDbContext CreateAuthDb()
        => new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(_authDbConn).Options);

    /// <summary>
    /// Creates a raw NpgsqlConnection to bot-db for SQL queries.
    /// Avoids coupling to Hermod.Bot project.
    /// </summary>
    public NpgsqlConnection CreateBotDbConnection()
        => new(_botDbConn);

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        DiscordApi.Stop();
        DiscordApi.Dispose();
    }
}
