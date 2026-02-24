using Hermod.Api.Auth;
using Hermod.Api.Tests.Auth;
using Hermod.Api.Tests.Infrastructure;
using Hermod.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Hermod.Api.Tests;

public class ApiFixture : WebApplicationFactory<Program>, IAsyncInitializer
{
    [ClassDataSource<HermodDatabase>(Shared = SharedType.PerTestSession)]
    public required HermodDatabase HermodDb { get; init; }

    [ClassDataSource<AuthDatabase>(Shared = SharedType.PerTestSession)]
    public required AuthDatabase AuthDb { get; init; }

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:hermod-db", HermodDb.ConnectionString);
        builder.UseSetting("ConnectionStrings:auth-db", AuthDb.ConnectionString);
        builder.UseSetting("Discord:ClientId", "test-client-id");
        builder.UseSetting("Discord:ClientSecret", "test-client-secret");

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }

    public HttpClient CreateAnonymousClient() => CreateClient();

    public HermodContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<HermodContext>();
    }

    public AuthDbContext CreateAuthDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    }
}
