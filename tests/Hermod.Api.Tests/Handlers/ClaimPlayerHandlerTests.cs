using Hermod.Api.Auth;
using Hermod.Api.Handlers;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class ClaimPlayerHandlerTests
{
    private static HermodContext CreateHermodDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    private static AuthDbContext CreateAuthDb()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options);
    }

    private static IServiceProvider BuildServices(AuthDbContext authDb)
    {
        var services = new ServiceCollection();
        services.AddSingleton(authDb);
        return services.BuildServiceProvider();
    }

    private static async Task<(HermodContext db, IServiceProvider services, Guid userId, string discordId, string playerUuid, Guid playId)>
        SetupRegisteredUserWithPlay()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var userId = Guid.NewGuid();
        var discordId = "123456789";
        var playerUuid = Guid.NewGuid().ToString();
        var playId = Guid.NewGuid();

        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(userId), DisplayName = "TestUser" });
        var playEntity = new PlayEntity
        {
            Id = PlayId.From(playId),
            UploadedById = UserId.From(Guid.NewGuid()),
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.Plays.Add(playEntity);
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(playId),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
            Score = "10",
        });
        await db.SaveChangesAsync();

        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = "TestUser",
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

        return (db, BuildServices(authDb), userId, discordId, playerUuid, playId);
    }

    [Test]
    public async Task Claim_RegisteredUser_ReturnsClaimed()
    {
        var (db, services, _, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.Claimed);
        await Assert.That(result.PlayerName).IsEqualTo("Alice");
    }

    [Test]
    public async Task Claim_CreatesPlayerMapping()
    {
        var (db, services, userId, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);
        await db.SaveChangesAsync();

        var mapping = await db.PlayerMappings.SingleAsync();
        await Assert.That(mapping.BgStatsPlayerUuid).IsEqualTo(playerUuid);
        await Assert.That(mapping.MappedUserId).IsEqualTo(UserId.From(userId));
    }

    [Test]
    public async Task Claim_SetsPlayerMappedUserId()
    {
        var (db, services, userId, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);
        await db.SaveChangesAsync();

        var player = await db.PlayPlayers.SingleAsync(pp => pp.PlayId == PlayId.From(playId));
        await Assert.That(player.MappedUserId).IsEqualTo(UserId.From(userId));
    }

    [Test]
    public async Task Claim_BackfillsOtherPlays()
    {
        var (db, services, userId, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        // Add a second play with the same player UUID
        var play2Id = Guid.NewGuid();
        db.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(play2Id),
            UploadedById = UserId.From(Guid.NewGuid()),
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Wingspan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(play2Id),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
        });
        await db.SaveChangesAsync();

        await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);
        await db.SaveChangesAsync();

        var play2Player = await db.PlayPlayers.SingleAsync(pp => pp.PlayId == PlayId.From(play2Id));
        await Assert.That(play2Player.MappedUserId).IsEqualTo(UserId.From(userId));
    }

    [Test]
    public async Task Claim_AlreadyClaimed_ReturnsAlreadyClaimed()
    {
        var (db, services, userId, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        // Pre-seed mapping
        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = playerUuid,
            MappedUserId = UserId.From(userId),
        });
        await db.SaveChangesAsync();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.AlreadyClaimed);
    }

    [Test]
    public async Task Claim_NotRegistered_ReturnsNotRegistered()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer("unknown-discord-id", Guid.NewGuid().ToString(), Guid.NewGuid()),
            db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.NotRegistered);
    }

    [Test]
    public async Task Claim_UnknownUuid_ReturnsPlayerNotFound()
    {
        var (db, services, _, discordId, _, playId) = await SetupRegisteredUserWithPlay();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, Guid.NewGuid().ToString(), playId), db, services);

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.PlayerNotFound);
    }

    [Test]
    public async Task Claim_Uploader_ReturnsIsUploader()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var userId = Guid.NewGuid();
        var discordId = "123456789";
        var playerUuid = Guid.NewGuid().ToString();
        var playId = Guid.NewGuid();

        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(userId), DisplayName = "Uploader" });
        db.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(playId),
            UploadedById = UserId.From(userId), // same user is the uploader
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(playId),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
        });
        await db.SaveChangesAsync();

        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = "Uploader",
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

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.IsUploader);
        await Assert.That(result.PlayerName).IsNull();
    }

    [Test]
    public async Task Claim_Claimed_ResultIncludesUserId()
    {
        var (db, services, userId, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.Claimed);
        await Assert.That(result.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task Claim_AlreadyClaimed_ResultIncludesUserId()
    {
        var (db, services, userId, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = playerUuid,
            MappedUserId = UserId.From(userId),
        });
        await db.SaveChangesAsync();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.AlreadyClaimed);
        await Assert.That(result.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task Claim_NotRegistered_UserIdIsNull()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer("unknown-discord-id", Guid.NewGuid().ToString(), Guid.NewGuid()),
            db, BuildServices(authDb));

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.NotRegistered);
        await Assert.That(result.UserId).IsNull();
    }

    [Test]
    public async Task Claim_RegisteredUser_CreatesProfileIfMissing()
    {
        var db = CreateHermodDb();
        var authDb = CreateAuthDb();

        var userId = Guid.NewGuid();
        var discordId = "999888777";
        var playerUuid = Guid.NewGuid().ToString();
        var playId = Guid.NewGuid();

        // No UserProfileEntity seeded
        var playEntity = new PlayEntity
        {
            Id = PlayId.From(playId),
            UploadedById = UserId.From(Guid.NewGuid()),
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Catan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.Plays.Add(playEntity);
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(playId),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
            Score = "10",
        });
        await db.SaveChangesAsync();

        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = "NewUser",
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

        var result = await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, BuildServices(authDb));
        await db.SaveChangesAsync();

        await Assert.That(result.Status).IsEqualTo(ClaimPlayerStatus.Claimed);

        var profile = await db.UserProfiles.SingleAsync(p => p.Id == UserId.From(userId));
        await Assert.That(profile.DisplayName).IsEqualTo("NewUser");
    }

    [Test]
    public async Task Claim_OnlyBackfillsNull_NoOverwrite()
    {
        var (db, services, userId, discordId, playerUuid, playId) = await SetupRegisteredUserWithPlay();

        // Add a second play where the player already has a different MappedUserId
        var otherUserId = UserId.From(Guid.NewGuid());
        var play2Id = Guid.NewGuid();
        db.Plays.Add(new PlayEntity
        {
            Id = PlayId.From(play2Id),
            UploadedById = UserId.From(Guid.NewGuid()),
            BgStatsPlayUuid = Guid.NewGuid().ToString(),
            GameName = "Wingspan",
            DatePlayed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        db.PlayPlayers.Add(new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = PlayId.From(play2Id),
            BgStatsPlayerUuid = playerUuid,
            PlayerName = "Alice",
            MappedUserId = otherUserId,
        });
        await db.SaveChangesAsync();

        await ClaimPlayerHandler.Handle(
            new ClaimPlayer(discordId, playerUuid, playId), db, services);
        await db.SaveChangesAsync();

        var play2Player = await db.PlayPlayers.SingleAsync(pp => pp.PlayId == PlayId.From(play2Id));
        await Assert.That(play2Player.MappedUserId).IsEqualTo(otherUserId);
    }
}
