using Hermod.Api.Tests.Infrastructure;
using Hermod.Auth;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Hermod.Api.Tests.Auth;

public class AuthDbFixture : IAsyncInitializer
{
    [ClassDataSource<AuthDatabase>(Shared = SharedType.PerTestSession)]
    public required AuthDatabase AuthDb { get; init; }

    private DbContextOptions<AuthDbContext> _options = null!;

    public Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(AuthDb.ConnectionString)
            .Options;

        using var ctx = new AuthDbContext(_options);
        ctx.Database.Migrate();
        return Task.CompletedTask;
    }

    public AuthDbContext CreateDbContext() => new(_options);
}
