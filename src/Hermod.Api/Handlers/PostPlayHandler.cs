using Discord;
using Discord.WebSocket;
using Hermod.Api.Messages;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class PostPlayHandler
{
    public static async Task Handle(
        PostPlay message,
        HermodContext db,
        DiscordSocketClient discord,
        ILogger logger)
    {
        var group = await db.Groups
            .FirstOrDefaultAsync(g => g.Id == GroupId.From(message.GroupId));

        if (group is null || !group.AllowSharing
            || group.DiscordPostChannelId is null || group.DiscordGuildId is null)
        {
            logger.LogDebug("Skipping embed for play {PlayId} — group not configured for sharing",
                message.PlayId);
            return;
        }

        var play = await db.Plays
            .Include(p => p.Players)
            .FirstOrDefaultAsync(p => p.Id == PlayId.From(message.PlayId));

        if (play is null)
        {
            logger.LogWarning("PostPlay: play {PlayId} not found in database", message.PlayId);
            return;
        }

        var channel = discord.GetChannel(group.DiscordPostChannelId.Value) as IMessageChannel;
        if (channel is null)
        {
            logger.LogWarning(
                "PostPlay: channel {ChannelId} not found or not a text channel",
                group.DiscordPostChannelId.Value);
            return;
        }

        var embed = BuildEmbed(play);
        var posted = await channel.SendMessageAsync(embed: embed);

        db.PlayPosts.Add(new PlayPostEntity
        {
            Id = PlayPostId.From(Guid.NewGuid()),
            PlayId = play.Id,
            DiscordGuildId = group.DiscordGuildId.Value,
            DiscordChannelId = group.DiscordPostChannelId.Value,
            DiscordMessageId = posted.Id,
            PostedAt = DateTime.UtcNow,
        });

        logger.LogInformation(
            "Posted embed for play {PlayId} to channel {ChannelId} (message {MessageId})",
            play.Id.Value, channel.Id, posted.Id);
    }

    private static Embed BuildEmbed(PlayEntity play)
    {
        var builder = new EmbedBuilder()
            .WithTitle(play.GameName)
            .WithColor(Color.Green)
            .WithFooter($"Played on {play.DatePlayed:yyyy-MM-dd}");

        if (play.BggGameId.HasValue)
            builder.WithUrl($"https://boardgamegeek.com/boardgame/{play.BggGameId.Value}");

        if (!string.IsNullOrEmpty(play.GameThumbnailUrl))
            builder.WithThumbnailUrl(play.GameThumbnailUrl);

        if (!string.IsNullOrEmpty(play.LocationName))
            builder.AddField("Location", play.LocationName, inline: true);

        if (play.Duration.HasValue && play.Duration.Value > TimeSpan.Zero)
            builder.AddField("Duration",
                $"{(int)play.Duration.Value.TotalHours:D2}:{play.Duration.Value.Minutes:D2}",
                inline: true);

        if (play.Rounds.HasValue && play.Rounds.Value > 0)
            builder.AddField("Rounds", play.Rounds.Value.ToString(), inline: true);

        var playerLines = play.Players
            .OrderBy(p => p.Rank ?? int.MaxValue)
            .Select(p =>
            {
                var line = p.Winner ? $"🏆 **{p.PlayerName}**" : p.PlayerName;
                if (p.Score is not null)
                    line += $" — {p.Score}";
                if (!string.IsNullOrEmpty(p.Role))
                    line += $" *(as {p.Role})*";
                return line;
            });

        builder.AddField("Players", string.Join("\n", playerLines));

        if (!string.IsNullOrEmpty(play.Comments))
            builder.WithDescription(play.Comments);

        return builder.Build();
    }
}
