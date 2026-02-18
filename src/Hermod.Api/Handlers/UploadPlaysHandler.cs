using Hermod.BGStats;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Handlers;

public static class UploadPlaysHandler
{
    public static async Task<List<PlayEntity>> Handle(
        UploadPlaysCommand command,
        HermodContext db,
        CancellationToken ct)
    {
        var result = await PlayFileParser.ParseAsync(command.FileStream, ct);
        var rawJson = await ReadStreamAsStringAsync(command.FileStream, ct);

        var entities = new List<PlayEntity>();

        foreach (var play in result.Plays)
        {
            var entity = new PlayEntity
            {
                Id = PlayId.From(Guid.NewGuid()),
                UploadedById = command.UploadedById,
                GroupId = command.GroupId,
                BgStatsPlayUuid = play.Uuid.ToString(),
                GameName = play.Game.Name,
                BggGameId = play.Game.BggId > 0 ? play.Game.BggId : null,
                GameThumbnailUrl = play.Game.ThumbnailUrl,
                DatePlayed = play.DatePlayed,
                Duration = play.Duration > TimeSpan.Zero ? play.Duration : null,
                LocationName = play.Location.Name,
                Rounds = play.Rounds > 0 ? play.Rounds : null,
                Comments = play.Comments,
                RawPlayFileJson = rawJson,
                ImageUrl = command.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                Players = play.Scores.Select(s => new PlayPlayerEntity
                {
                    Id = PlayPlayerId.From(Guid.NewGuid()),
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
                }).ToList(),
            };

            await ResolvePlayerMappingsAsync(db, command.UploadedById, entity.Players, ct);
            db.Plays.Add(entity);
            entities.Add(entity);
        }

        return entities;
    }

    private static async Task ResolvePlayerMappingsAsync(
        HermodContext db,
        UserId ownerId,
        List<PlayPlayerEntity> players,
        CancellationToken ct)
    {
        var playerUuids = players.Select(p => p.BgStatsPlayerUuid).ToList();
        var mappings = await db.PlayerMappings
            .Where(pm => pm.OwnerUserId == ownerId && playerUuids.Contains(pm.BgStatsPlayerUuid))
            .ToDictionaryAsync(pm => pm.BgStatsPlayerUuid, pm => pm.MappedUserId, ct);

        foreach (var player in players)
        {
            if (mappings.TryGetValue(player.BgStatsPlayerUuid, out var mappedUserId))
            {
                player.MappedUserId = mappedUserId;
            }
        }
    }

    private static async Task<string> ReadStreamAsStringAsync(Stream stream, CancellationToken ct)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync(ct);
    }
}
