using Hermod.Api.Messages;
using Hermod.BGStats.Models;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Handlers;

public static class PlayExtractedHandler
{
    public static async Task<PlayCreated> Handle(PlayExtracted message, HermodContext db)
    {
        var play = message.ParsedPlay;

        UserId? uploadedById = null;
        if (message.SenderDiscordId is not null)
        {
            uploadedById = await FindOrCreateUserAsync(db, message.SenderDiscordId);
        }

        var entity = new PlayEntity
        {
            Id = PlayId.From(Guid.NewGuid()),
            UploadedById = uploadedById,
            GroupId = message.GroupId.HasValue ? GroupId.From(message.GroupId.Value) : null,
            BgStatsPlayUuid = play.Uuid.ToString(),
            GameName = play.Game.Name,
            BggGameId = play.Game.BggId > 0 ? play.Game.BggId : null,
            GameThumbnailUrl = string.IsNullOrEmpty(play.Game.ThumbnailUrl) ? null : play.Game.ThumbnailUrl,
            DatePlayed = play.DatePlayed,
            Duration = play.Duration > TimeSpan.Zero ? play.Duration : null,
            LocationName = string.IsNullOrEmpty(play.Location.Name) ? null : play.Location.Name,
            Rounds = play.Rounds > 0 ? play.Rounds : null,
            Comments = play.Comments,
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

        db.Plays.Add(entity);

        return new PlayCreated(entity.Id.Value, message.GroupId);
    }

    private static async Task<UserId> FindOrCreateUserAsync(HermodContext db, string discordId)
    {
        const string provider = "Discord";

        var login = await db.UserExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == discordId);

        if (login is not null)
        {
            return login.UserId;
        }

        var user = new UserEntity
        {
            Id = UserId.From(Guid.NewGuid()),
            DisplayName = $"Discord:{discordId}",   // placeholder until profile is fetched
        };

        db.Users.Add(user);

        db.UserExternalLogins.Add(new UserExternalLoginEntity
        {
            Id = ExternalLoginId.From(Guid.NewGuid()),
            UserId = user.Id,
            Provider = provider,
            ProviderKey = discordId,
        });

        return user.Id;
    }
}
