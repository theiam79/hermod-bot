using Hermod.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Handlers;

public static class ClaimPlayerHandler
{
    // IExternalUserResolver resolved via IServiceProvider so Wolverine sees only HermodContext
    // for transaction management (AuthDbContext is a transitive dependency of ExternalUserResolver).
    public static async Task<ClaimPlayerResult> Handle(
        ClaimPlayer message, HermodContext db, IServiceProvider services)
    {
        var userResolver = services.GetRequiredService<IExternalUserResolver>();
        var externalUser = await userResolver.ResolveAsync(message.Provider, message.ProviderKey);
        if (externalUser is null)
            return new ClaimPlayerResult(ClaimPlayerStatus.NotRegistered, null, null);

        var userId = UserId.From(externalUser.UserId);

        var hasProfile = await db.UserProfiles.AnyAsync(p => p.Id == userId);
        if (!hasProfile)
        {
            db.UserProfiles.Add(new UserProfileEntity
            {
                Id = userId,
                DisplayName = externalUser.DisplayName,
            });
        }

        var play = await db.Plays.FindAsync(PlayId.From(message.PlayId));
        if (play?.UploadedById == userId)
            return new ClaimPlayerResult(ClaimPlayerStatus.IsUploader, null, externalUser.UserId);

        var hasPlayer = await db.PlayPlayers
            .AnyAsync(pp => pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid);
        if (!hasPlayer)
            return new ClaimPlayerResult(ClaimPlayerStatus.PlayerNotFound, null, externalUser.UserId);

        var alreadyMapped = await db.PlayerMappings
            .AnyAsync(pm => pm.BgStatsPlayerUuid == message.BgStatsPlayerUuid && pm.MappedUserId == userId);
        if (alreadyMapped)
            return new ClaimPlayerResult(ClaimPlayerStatus.AlreadyClaimed, null, externalUser.UserId);

        db.PlayerMappings.Add(new PlayerMappingEntity
        {
            Id = PlayerMappingId.From(Guid.NewGuid()),
            BgStatsPlayerUuid = message.BgStatsPlayerUuid,
            MappedUserId = userId,
        });

        // Set MappedUserId on the specific play's player
        var playPlayer = await db.PlayPlayers
            .FirstOrDefaultAsync(pp => pp.PlayId == PlayId.From(message.PlayId)
                                    && pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid
                                    && pp.MappedUserId == null);
        if (playPlayer is not null)
            playPlayer.MappedUserId = userId;

        // Backfill all other plays where this UUID has no mapping yet
        var unlinkedPlayers = await db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid
                      && pp.MappedUserId == null
                      && pp.PlayId != PlayId.From(message.PlayId))
            .ToListAsync();

        foreach (var p in unlinkedPlayers)
            p.MappedUserId = userId;

        var playerName = await db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == message.BgStatsPlayerUuid)
            .Select(pp => pp.PlayerName)
            .FirstAsync();

        return new ClaimPlayerResult(ClaimPlayerStatus.Claimed, playerName, externalUser.UserId);
    }
}
