# Task 03 — PostPlayHandler: Build and Send Embed

## Why
`PostPlayHandler` is the terminal step in the play upload pipeline. It reads the play and
group from the database, checks whether the group is configured for sharing, builds a Discord
embed, posts it to the configured channel, and records the message ID in `PlayPosts`.

## Steps

### Create `src/Hermod.Api/Handlers/PostPlayHandler.cs`

```csharp
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

        if (group is null || !group.AllowSharing || group.DiscordPostChannelId is null)
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
            DiscordGuildId = group.DiscordGuildId!.Value,
            DiscordChannelId = group.DiscordPostChannelId!.Value,
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
```

## Notes
- `DiscordSocketClient` is injected as a Wolverine handler parameter. It is registered as a
  singleton by `Discord.Addons.Hosting`, so Wolverine can resolve it directly.
- `discord.GetChannel(id)` returns `null` if the channel is not in the bot's cache. This can
  happen if the channel was deleted or the bot lost access. The null-guard prevents a crash.
- `BuildEmbed` is private and static — no external dependencies, easy to unit-test by
  constructing a `PlayEntity` directly.
- **Player ordering**: sorted by rank ascending (so 1st place appears first). Players with
  no rank are sorted last.
- **Winner indicator**: 🏆 before the player name. If multiple players have `Winner = true`
  (team games), all get the indicator.
- **Score display**: uses the raw `Score` string (the expression, e.g. `"42"` or `"3*10"`).
  `CalculatedScore` could be shown as a tooltip or parenthetical if preferred.
- `WithFooter` uses the date rather than "posted by X" because uploader attribution is not
  always available (anonymous uploads). Uploader info can be added when user identity is solid.
- The `PlayPostEntity` insert is part of the same Wolverine unit of work as this handler, so
  it commits atomically with no extra `SaveChangesAsync` needed.

## Acceptance Criteria
- [ ] `src/Hermod.Api/Handlers/PostPlayHandler.cs` exists
- [ ] `dotnet build Hermod.slnx` succeeds
- [ ] Uploading a play to a group with `AllowSharing = true` and a configured channel posts
      an embed in that channel
- [ ] The embed includes game name, date, all players, winner indicator, and score
- [ ] A row is inserted into `PlayPosts` with the correct `DiscordMessageId`
- [ ] Groups with `AllowSharing = false` do not post embeds
- [ ] Groups with no `DiscordPostChannelId` do not post embeds (no exception)
- [ ] A missing or inaccessible channel logs a warning and does not throw
