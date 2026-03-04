using Testcontainers.PostgreSql;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Hermod.Api.Tests.Infrastructure;

public class AuthDatabase : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<ContainerRuntime>(Shared = SharedType.PerTestSession)]
    public required ContainerRuntime Runtime { get; init; }

    private PostgreSqlContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Container not started.");

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("auth")
            .Build();
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
