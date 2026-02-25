using Hermod.Api.Handlers;
using Hermod.Api.Messages;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Hermod.Api.Tests.Handlers;

public class DistributeFileHandlerTests
{
    private static HermodContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<HermodContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HermodContext(options);
    }

    /// <summary>
    /// Minimal valid .bgsplay JSON with the given player UUIDs.
    /// MePlayerUuid is player at index 0 (refId 1).
    /// </summary>
    private static string BuildBgsPlayJson(params Guid[] playerUuids)
    {
        var players = playerUuids.Select((uuid, i) => $$"""
            { "id": {{i + 1}}, "uuid": "{{uuid}}", "name": "Player{{i + 1}}", "isAnonymous": 0, "modificationDate": "2026-01-15 00:00:00" }
        """).ToList();

        var scores = playerUuids.Select((_, i) => $$"""
            { "playerRefId": {{i + 1}}, "score": "10", "winner": 0, "newPlayer": 0, "startPlayer": 0, "rank": {{i + 1}}, "seatOrder": {{i + 1}} }
        """).ToList();

        return $$"""
        {
            "about": "test",
            "players": [ {{string.Join(", ", players)}} ],
            "locations": [ { "id": 1, "uuid": "{{Guid.NewGuid()}}", "name": "Home", "modificationDate": "2026-01-15 00:00:00" } ],
            "games": [ { "id": 1, "uuid": "{{Guid.NewGuid()}}", "name": "Catan", "modificationDate": "2026-01-15 00:00:00", "bggId": 13, "highestWins": 1, "noPoints": 0, "cooperative": 0, "usesTeams": 0, "isBaseGame": 1, "isExpansion": 0 } ],
            "plays": [ { "uuid": "{{Guid.NewGuid()}}", "playDate": "2026-01-15 00:00:00", "modificationDate": "2026-01-15 00:00:00", "entryDate": "2026-01-15 00:00:00", "gameRefId": 1, "locationRefId": 1, "durationMin": 60, "usesTeams": 0, "ignored": 0, "manualWinner": 0, "rounds": 0, "scoringSetting": 0, "playerScores": [ {{string.Join(", ", scores)}} ], "expansionPlays": [] } ],
            "userInfo": { "meRefId": 1 }
        }
        """;
    }

    private static async Task<(HermodContext db, UploadEntity upload, Guid mePlayerUuid, Guid otherPlayerUuid, Guid uploaderId)>
        SetupUploadWithTwoPlayers()
    {
        var db = CreateInMemoryDb();
        var mePlayerUuid = Guid.NewGuid();
        var otherPlayerUuid = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();
        var uploadId = UploadId.From(Guid.NewGuid());

        var upload = new UploadEntity
        {
            Id = uploadId,
            UploadedById = UserId.From(uploaderId),
            FileContent = BuildBgsPlayJson(mePlayerUuid, otherPlayerUuid),
            FileName = "test.bgsplay",
            CreatedAt = DateTime.UtcNow,
        };
        db.Uploads.Add(upload);
        await db.SaveChangesAsync();

        return (db, upload, mePlayerUuid, otherPlayerUuid, uploaderId);
    }

    [Test]
    public async Task EmitsForMappedSubscribedUser()
    {
        var (db, upload, mePlayerUuid, otherPlayerUuid, uploaderId) = await SetupUploadWithTwoPlayers();

        var recipientId = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(recipientId),
            DisplayName = "Recipient",
            SubscribeToPlays = true,
        });
        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = otherPlayerUuid.ToString(),
            MappedUserId = UserId.From(recipientId),
        });
        await db.SaveChangesAsync();

        var message = new PlayFileUploaded(upload.Id.Value, mePlayerUuid, uploaderId);
        var (continuation, entity) = await DistributeFileHandler.LoadAsync(message, db);
        var result = await DistributeFileHandler.Handle(message, entity!, db);

        var distributed = result.OfType<DistributePlayFile>().ToList();
        await Assert.That(distributed).Count().IsEqualTo(1);
        await Assert.That(distributed[0].RecipientUserId).IsEqualTo(recipientId);
        await Assert.That(distributed[0].FileContent).IsEqualTo(upload.FileContent);
        await Assert.That(distributed[0].FileName).IsEqualTo("test.bgsplay");
    }

    [Test]
    public async Task NoMappings_EmitsNothing()
    {
        var (db, upload, mePlayerUuid, _, uploaderId) = await SetupUploadWithTwoPlayers();

        var message = new PlayFileUploaded(upload.Id.Value, mePlayerUuid, uploaderId);
        var (_, entity) = await DistributeFileHandler.LoadAsync(message, db);
        var result = await DistributeFileHandler.Handle(message, entity!, db);

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task OptedOut_EmitsNothing()
    {
        var (db, upload, mePlayerUuid, otherPlayerUuid, uploaderId) = await SetupUploadWithTwoPlayers();

        var recipientId = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(recipientId),
            DisplayName = "OptedOut",
            SubscribeToPlays = false,
        });
        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = otherPlayerUuid.ToString(),
            MappedUserId = UserId.From(recipientId),
        });
        await db.SaveChangesAsync();

        var message = new PlayFileUploaded(upload.Id.Value, mePlayerUuid, uploaderId);
        var (_, entity) = await DistributeFileHandler.LoadAsync(message, db);
        var result = await DistributeFileHandler.Handle(message, entity!, db);

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UploadNotFound_Stops()
    {
        var db = CreateInMemoryDb();

        var message = new PlayFileUploaded(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (continuation, entity) = await DistributeFileHandler.LoadAsync(message, db);

        await Assert.That(continuation).IsEqualTo(Wolverine.HandlerContinuation.Stop);
        await Assert.That(entity).IsNull();
    }

    [Test]
    public async Task DeduplicatesByUserId()
    {
        var db = CreateInMemoryDb();
        var mePlayerUuid = Guid.NewGuid();
        var player2Uuid = Guid.NewGuid();
        var player3Uuid = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();
        var uploadId = UploadId.From(Guid.NewGuid());

        // File with 3 players: me + two others that both map to the same user
        db.Uploads.Add(new UploadEntity
        {
            Id = uploadId,
            UploadedById = UserId.From(uploaderId),
            FileContent = BuildBgsPlayJson(mePlayerUuid, player2Uuid, player3Uuid),
            FileName = "test.bgsplay",
            CreatedAt = DateTime.UtcNow,
        });

        var recipientId = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(recipientId),
            DisplayName = "Recipient",
            SubscribeToPlays = true,
        });
        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = player2Uuid.ToString(),
            MappedUserId = UserId.From(recipientId),
        });
        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = player3Uuid.ToString(),
            MappedUserId = UserId.From(recipientId),
        });
        await db.SaveChangesAsync();

        var message = new PlayFileUploaded(uploadId.Value, mePlayerUuid, uploaderId);
        var (_, entity) = await DistributeFileHandler.LoadAsync(message, db);
        var result = await DistributeFileHandler.Handle(message, entity!, db);

        await Assert.That(result.OfType<DistributePlayFile>()).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ExcludesMePlayer()
    {
        var (db, upload, mePlayerUuid, _, uploaderId) = await SetupUploadWithTwoPlayers();

        // Map the me player to a user — should NOT receive distribution
        var meUserId = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(meUserId),
            DisplayName = "MeUser",
            SubscribeToPlays = true,
        });
        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = mePlayerUuid.ToString(),
            MappedUserId = UserId.From(meUserId),
        });
        await db.SaveChangesAsync();

        var message = new PlayFileUploaded(upload.Id.Value, mePlayerUuid, uploaderId);
        var (_, entity) = await DistributeFileHandler.LoadAsync(message, db);
        var result = await DistributeFileHandler.Handle(message, entity!, db);

        // Me player should be excluded even though mapped and subscribed
        var distributed = result.OfType<DistributePlayFile>().ToList();
        await Assert.That(distributed).Count().IsEqualTo(0);
    }
}
