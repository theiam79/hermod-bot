using Hermod.BGStats;
using Hermod.BGStats.Models;
using Hermod.Core.Mappers;
using Hermod.Core.Models;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Core.Services;

public class PlayService(IDbContextFactory<HermodContext> contextFactory)
{
    public async Task<List<PlaySummary>> UploadPlaysAsync(
        Stream fileStream,
        UserId uploadedById,
        GroupId? groupId,
        string? imageUrl,
        CancellationToken ct = default)
    {
        var result = await PlayFileParser.ParseAsync(fileStream, ct);
        var rawJson = await ReadStreamAsStringAsync(fileStream, ct);

        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var summaries = new List<PlaySummary>();

        foreach (var play in result.Plays)
        {
            var entity = new PlayEntity
            {
                Id = PlayId.From(Guid.NewGuid()),
                UploadedById = uploadedById,
                GroupId = groupId,
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
                ImageUrl = imageUrl,
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

            // Resolve player mappings
            await ResolvePlayerMappingsAsync(context, uploadedById, entity.Players, ct);

            context.Plays.Add(entity);
            summaries.Add(PlayMapper.ToSummary(entity));
        }

        await context.SaveChangesAsync(ct);
        return summaries;
    }

    public async Task<PlaySummary?> GetPlayAsync(PlayId id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var entity = await context.Plays
            .Include(p => p.Players)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return entity is null ? null : PlayMapper.ToSummary(entity);
    }

    public async Task<List<PlaySummary>> GetPlaysForGroupAsync(GroupId groupId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var entities = await context.Plays
            .Include(p => p.Players)
            .Where(p => p.GroupId == groupId)
            .OrderByDescending(p => p.DatePlayed)
            .ToListAsync(ct);

        return entities.Select(PlayMapper.ToSummary).ToList();
    }

    public async Task<List<PlaySummary>> GetPlaysForUserAsync(UserId userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var entities = await context.Plays
            .Include(p => p.Players)
            .Where(p => p.UploadedById == userId)
            .OrderByDescending(p => p.DatePlayed)
            .ToListAsync(ct);

        return entities.Select(PlayMapper.ToSummary).ToList();
    }

    private static async Task ResolvePlayerMappingsAsync(
        HermodContext context,
        UserId ownerId,
        List<PlayPlayerEntity> players,
        CancellationToken ct)
    {
        var playerUuids = players.Select(p => p.BgStatsPlayerUuid).ToList();
        var mappings = await context.PlayerMappings
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
