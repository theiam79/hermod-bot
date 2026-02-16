using Hermod.Core.Models;
using Hermod.Data.Entities;

namespace Hermod.Core.Mappers;

public static class PlayMapper
{
    public static PlaySummary ToSummary(PlayEntity entity)
    {
        return new PlaySummary
        {
            Id = entity.Id,
            GameName = entity.GameName,
            BggGameId = entity.BggGameId,
            GameThumbnailUrl = entity.GameThumbnailUrl,
            DatePlayed = entity.DatePlayed,
            Duration = entity.Duration,
            LocationName = entity.LocationName,
            Rounds = entity.Rounds,
            Comments = entity.Comments,
            ImageUrl = entity.ImageUrl,
            CreatedAt = entity.CreatedAt,
            Players = entity.Players.Select(ToPlayerSummary).ToList(),
        };
    }

    private static PlayPlayerSummary ToPlayerSummary(PlayPlayerEntity entity)
    {
        return new PlayPlayerSummary
        {
            PlayerName = entity.PlayerName,
            BgStatsPlayerUuid = entity.BgStatsPlayerUuid,
            MappedUserId = entity.MappedUserId,
            Score = entity.Score,
            CalculatedScore = entity.CalculatedScore,
            Winner = entity.Winner,
            Rank = entity.Rank,
            Role = entity.Role,
            Team = entity.Team,
        };
    }
}
