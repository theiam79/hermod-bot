using Hermod.Auth;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Auth;

public class ExternalUserResolverTests
{
    [ClassDataSource<AuthDbFixture>(Shared = SharedType.PerTestSession)]
    public required AuthDbFixture Fixture { get; init; }

    private async Task<(Guid userId, string providerKey)> SeedUser(AuthDbContext db, string displayName)
    {
        var userId = Guid.NewGuid();
        var key = $"resolver-{Guid.NewGuid()}";

        db.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        db.ExternalLogins.Add(new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Discord",
            ProviderKey = key,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return (userId, key);
    }

    [Test]
    public async Task ExistingLogin_ReturnsUserInfo()
    {
        await using var db = Fixture.CreateDbContext();
        var (userId, key) = await SeedUser(db, "TestUser");
        var resolver = new ExternalUserResolver(db);

        var result = await resolver.ResolveAsync("Discord", key);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UserId).IsEqualTo(userId);
        await Assert.That(result.DisplayName).IsEqualTo("TestUser");
    }

    [Test]
    public async Task NoMatchingLogin_ReturnsNull()
    {
        await using var db = Fixture.CreateDbContext();
        var resolver = new ExternalUserResolver(db);

        var result = await resolver.ResolveAsync("Discord", $"nonexistent-{Guid.NewGuid()}");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task WrongProvider_ReturnsNull()
    {
        await using var db = Fixture.CreateDbContext();
        var (_, key) = await SeedUser(db, "ProviderTest");
        var resolver = new ExternalUserResolver(db);

        var result = await resolver.ResolveAsync("GitHub", key);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task WrongProviderKey_ReturnsNull()
    {
        await using var db = Fixture.CreateDbContext();
        await SeedUser(db, "KeyTest");
        var resolver = new ExternalUserResolver(db);

        var result = await resolver.ResolveAsync("Discord", $"wrong-key-{Guid.NewGuid()}");

        await Assert.That(result).IsNull();
    }
}
