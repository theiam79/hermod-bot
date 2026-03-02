using Hermod.Api.Messages;
using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Handlers;

public static class PlayExtractedHandler
{
    public static async Task<PlayPersisted> Handle(PlayExtracted message, HermodContext db, TimeProvider timeProvider)
    {
        var play = message.ParsedPlay;
        var bgStatsUuid = play.Uuid.ToString();
        var uploadedById = UserId.From(message.UploadedById);

        var existing = await db.Plays
            .Include(p => p.Players)
            .FirstOrDefaultAsync(p => p.BgStatsPlayUuid == bgStatsUuid
                                   && p.UploadedById == uploadedById);

        PlayChangeType changeType;
        PlayEntity entity;

        if (existing is null)
        {
            var playId = PlayId.From(Guid.NewGuid());
            entity = new PlayEntity
            {
                Id = playId,
                UploadedById = uploadedById,
                UploadId = message.UploadId.HasValue ? UploadId.From(message.UploadId.Value) : null,
                BgStatsPlayUuid = bgStatsUuid,
                GameName = play.Game.Name,
                BggGameId = play.Game.BggId > 0 ? play.Game.BggId : null,
                GameThumbnailUrl = string.IsNullOrEmpty(play.Game.ThumbnailUrl) ? null : play.Game.ThumbnailUrl,
                DatePlayed = DateTime.SpecifyKind(play.DatePlayed, DateTimeKind.Utc),
                Duration = play.Duration > TimeSpan.Zero ? play.Duration : null,
                LocationName = string.IsNullOrEmpty(play.Location.Name) ? null : play.Location.Name,
                Rounds = play.Rounds > 0 ? play.Rounds : null,
                Comments = play.Comments,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                Players = CreatePlayers(play, playId),
            };

            db.Plays.Add(entity);
            changeType = PlayChangeType.Created;
        }
        else
        {
            entity = existing;
            entity.UploadId = message.UploadId.HasValue ? UploadId.From(message.UploadId.Value) : null;
            entity.GameName = play.Game.Name;
            entity.BggGameId = play.Game.BggId > 0 ? play.Game.BggId : null;
            entity.GameThumbnailUrl = string.IsNullOrEmpty(play.Game.ThumbnailUrl) ? null : play.Game.ThumbnailUrl;
            entity.DatePlayed = DateTime.SpecifyKind(play.DatePlayed, DateTimeKind.Utc);
            entity.Duration = play.Duration > TimeSpan.Zero ? play.Duration : null;
            entity.LocationName = string.IsNullOrEmpty(play.Location.Name) ? null : play.Location.Name;
            entity.Rounds = play.Rounds > 0 ? play.Rounds : null;
            entity.Comments = play.Comments;
            entity.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

            // Replace players
            db.PlayPlayers.RemoveRange(entity.Players);
            entity.Players = CreatePlayers(play, entity.Id);

            changeType = PlayChangeType.Updated;
        }

        // Auto-link uploader's "me" player
        if (message.MePlayerUuid.HasValue)
        {
            var meUuidStr = message.MePlayerUuid.Value.ToString();
            var alreadyLinked = await db.PlayerMappings.AnyAsync(
                pm => pm.BgStatsPlayerUuid == meUuidStr && pm.MappedUserId == uploadedById);
            if (!alreadyLinked)
            {
                db.PlayerMappings.Add(new PlayerMappingEntity
                {
                    Id = PlayerMappingId.From(Guid.NewGuid()),
                    BgStatsPlayerUuid = meUuidStr,
                    MappedUserId = uploadedById,
                });

                // Set directly — the mapping isn't queryable until SaveChanges
                var mePlayer = entity.Players.FirstOrDefault(p => p.BgStatsPlayerUuid == meUuidStr);
                if (mePlayer is not null)
                    mePlayer.MappedUserId = uploadedById;
            }
        }

        // Resolve MappedUserId on players from existing PlayerMappings
        var playerUuids = entity.Players.Select(p => p.BgStatsPlayerUuid).ToList();
        var mappings = await db.PlayerMappings
            .Where(pm => playerUuids.Contains(pm.BgStatsPlayerUuid))
            .ToListAsync();

        if (mappings.Count > 0)
        {
            var mappingLookup = mappings
                .GroupBy(m => m.BgStatsPlayerUuid)
                .ToDictionary(g => g.Key, g => g.First().MappedUserId);

            foreach (var player in entity.Players)
            {
                if (player.MappedUserId is null && mappingLookup.TryGetValue(player.BgStatsPlayerUuid, out var userId))
                {
                    player.MappedUserId = userId;
                }
            }
        }

        return new PlayPersisted(entity.Id.Value, message.UploadedById, changeType);
    }

    private static List<PlayPlayerEntity> CreatePlayers(BGStats.Models.Play play, PlayId playId) =>
        play.Scores.Select(s => new PlayPlayerEntity
        {
            Id = PlayPlayerId.From(Guid.NewGuid()),
            PlayId = playId,
            BgStatsPlayerUuid = s.Player.Uuid.ToString(),
            PlayerName = s.Player.Name,
            Score = s.ScoreExpression,
            CalculatedScore = s.CalculateScore(),
            Winner = s.Winner,
            Rank = s.Rank > 0 ? s.Rank : null,
            Role = string.IsNullOrEmpty(s.Role) ? null : s.Role,
            Team = s.Team,
            NewPlayer = s.NewPlayer,
            StartPlayer = s.StartPlayer,
        }).ToList();
}
