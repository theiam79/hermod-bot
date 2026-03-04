using Hermod.Auth;
using Hermod.Api.Features.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

/// <summary>
/// Verifies ClaimPlayerHandler behavior related to ClaimChanged emission.
/// The handler publishes ClaimChanged via IMessageBus.GetService (optional).
/// Without IMessageBus registered, the handler still succeeds — verifying
/// that the publish path is conditional and doesn't break the claim flow.
/// Full emission verification is covered by E2E tests.
/// </summary>
public class ClaimPlayerEmitsClaimChangedTests
{
    [Test]
    public async Task Claim_SucceedsWithoutMessageBus()
    {
        var hermodDb = new HermodContext(new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var authDb = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var userId = Guid.NewGuid();
        var discordId = "555666777";
        var playerUuid = Guid.NewGuid().ToString();
        var playId = Guid.NewGuid();

        hermodDb.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(userId), DisplayName = "Test" });
        hermodDb.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(playId),
            UploadedById = UserId.From(Guid.NewGuid()),
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        hermodDb.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(playId),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
        });
        await hermodDb.SaveChangesAsync();

        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        authDb.ExternalLogins.Add(new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Discord",
            ProviderKey = discordId,
            CreatedAt = DateTime.UtcNow,
        });
        await authDb.SaveChangesAsync();

        // No IMessageBus registered — handler should still succeed
        var services = new ServiceCollection();
        services.AddSingleton<IExternalUserResolver>(new ExternalUserResolver(authDb));
        var sp = services.BuildServiceProvider();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer("Discord", discordId, playerUuid, playId), hermodDb, sp);

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.Claimed);
        await Assert.That(result.PlayerName).IsEqualTo("Alice");
    }
}
