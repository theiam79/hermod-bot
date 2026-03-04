using Hermod.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TUnit.Core;

namespace Hermod.Api.Tests.Auth;

public class ExternalLoginServiceTests
{
    [ClassDataSource<AuthDbFixture>(Shared = SharedType.PerTestSession)]
    public required AuthDbFixture Fixture { get; init; }

    [Test]
    public async Task FirstLogin_CreatesAuthUserAndExternalLogin()
    {
        await using var db = Fixture.CreateDbContext();
        var tp = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        var svc = new ExternalLoginService(db, tp);
        var key = $"first-login-{Guid.NewGuid()}";

        var (userId, isNew) = await svc.ProvisionOrUpdateAsync("Discord", key, "Alice", "https://cdn.example.com/alice.png");

        var user = await db.Users.FindAsync(userId);
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.DisplayName).IsEqualTo("Alice");

        var login = await db.ExternalLogins.FirstAsync(e => e.ProviderKey == key);
        await Assert.That(login.Provider).IsEqualTo("Discord");
        await Assert.That(login.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task FirstLogin_SetsTimestampsFromTimeProvider()
    {
        await using var db = Fixture.CreateDbContext();
        var now = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var tp = new FakeTimeProvider(now);
        var svc = new ExternalLoginService(db, tp);
        var key = $"timestamps-{Guid.NewGuid()}";

        var (userId, _) = await svc.ProvisionOrUpdateAsync("Discord", key, "Bob", null);

        var user = await db.Users.FindAsync(userId);
        await Assert.That(user!.CreatedAt).IsEqualTo(now.UtcDateTime);
        await Assert.That(user.LastLoginAt).IsEqualTo(now.UtcDateTime);

        var login = await db.ExternalLogins.FirstAsync(e => e.ProviderKey == key);
        await Assert.That(login.CreatedAt).IsEqualTo(now.UtcDateTime);
    }

    [Test]
    public async Task FirstLogin_StoresProviderAndProviderKey()
    {
        await using var db = Fixture.CreateDbContext();
        var tp = new FakeTimeProvider();
        var svc = new ExternalLoginService(db, tp);
        var key = $"provider-{Guid.NewGuid()}";

        await svc.ProvisionOrUpdateAsync("Discord", key, "Carol", null);

        var login = await db.ExternalLogins.FirstAsync(e => e.ProviderKey == key);
        await Assert.That(login.Provider).IsEqualTo("Discord");
        await Assert.That(login.ProviderKey).IsEqualTo(key);
    }

    [Test]
    public async Task FirstLogin_ReturnsIsNewTrue()
    {
        await using var db = Fixture.CreateDbContext();
        var tp = new FakeTimeProvider();
        var svc = new ExternalLoginService(db, tp);
        var key = $"is-new-{Guid.NewGuid()}";

        var (_, isNew) = await svc.ProvisionOrUpdateAsync("Discord", key, "Dave", null);

        await Assert.That(isNew).IsTrue();
    }

    [Test]
    public async Task ReturningLogin_UpdatesLastLoginAt()
    {
        await using var db = Fixture.CreateDbContext();
        var initial = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var tp = new FakeTimeProvider(initial);
        var svc = new ExternalLoginService(db, tp);
        var key = $"returning-last-login-{Guid.NewGuid()}";

        var (userId, _) = await svc.ProvisionOrUpdateAsync("Discord", key, "Eve", null);

        tp.SetUtcNow(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        await svc.ProvisionOrUpdateAsync("Discord", key, "Eve", null);

        var user = await db.Users.FindAsync(userId);
        await Assert.That(user!.LastLoginAt).IsEqualTo(tp.GetUtcNow().UtcDateTime);
        await Assert.That(user.CreatedAt).IsEqualTo(initial.UtcDateTime);
    }

    [Test]
    public async Task ReturningLogin_SyncsDisplayName()
    {
        await using var db = Fixture.CreateDbContext();
        var tp = new FakeTimeProvider();
        var svc = new ExternalLoginService(db, tp);
        var key = $"sync-name-{Guid.NewGuid()}";

        var (userId, _) = await svc.ProvisionOrUpdateAsync("Discord", key, "OldName", null);

        tp.Advance(TimeSpan.FromHours(1));
        await svc.ProvisionOrUpdateAsync("Discord", key, "NewName", null);

        var user = await db.Users.FindAsync(userId);
        await Assert.That(user!.DisplayName).IsEqualTo("NewName");
    }

    [Test]
    public async Task ReturningLogin_UpdatesAvatarUrl_WhenNotNull()
    {
        await using var db = Fixture.CreateDbContext();
        var tp = new FakeTimeProvider();
        var svc = new ExternalLoginService(db, tp);
        var key = $"avatar-update-{Guid.NewGuid()}";

        var (userId, _) = await svc.ProvisionOrUpdateAsync("Discord", key, "Frank", "https://old.png");

        tp.Advance(TimeSpan.FromHours(1));
        await svc.ProvisionOrUpdateAsync("Discord", key, "Frank", "https://new.png");

        var user = await db.Users.FindAsync(userId);
        await Assert.That(user!.AvatarUrl).IsEqualTo("https://new.png");
    }

    [Test]
    public async Task ReturningLogin_PreservesAvatarUrl_WhenNull()
    {
        await using var db = Fixture.CreateDbContext();
        var tp = new FakeTimeProvider();
        var svc = new ExternalLoginService(db, tp);
        var key = $"avatar-preserve-{Guid.NewGuid()}";

        var (userId, _) = await svc.ProvisionOrUpdateAsync("Discord", key, "Grace", "https://original.png");

        tp.Advance(TimeSpan.FromHours(1));
        await svc.ProvisionOrUpdateAsync("Discord", key, "Grace", null);

        var user = await db.Users.FindAsync(userId);
        await Assert.That(user!.AvatarUrl).IsEqualTo("https://original.png");
    }

    [Test]
    public async Task ReturningLogin_ReturnsIsNewFalse()
    {
        await using var db = Fixture.CreateDbContext();
        var tp = new FakeTimeProvider();
        var svc = new ExternalLoginService(db, tp);
        var key = $"not-new-{Guid.NewGuid()}";

        await svc.ProvisionOrUpdateAsync("Discord", key, "Hank", null);

        tp.Advance(TimeSpan.FromHours(1));
        var (_, isNew) = await svc.ProvisionOrUpdateAsync("Discord", key, "Hank", null);

        await Assert.That(isNew).IsFalse();
    }
}
