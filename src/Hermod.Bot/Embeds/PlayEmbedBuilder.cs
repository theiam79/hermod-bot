using Hermod.Messages;
using NetCord;
using NetCord.Rest;
using System.Text;

namespace Hermod.Bot.Embeds;

public static class PlayEmbedBuilder
{
    private static readonly Color PlayColor = new(0x57F287); // Discord green

    public static EmbedProperties Build(PlaySnapshot snapshot)
    {
        return new EmbedProperties
        {
            Title = snapshot.GameName,
            Description = BuildDescription(snapshot),
            Thumbnail = snapshot.GameThumbnailUrl is { } url ? new EmbedThumbnailProperties(url) : null,
            Timestamp = snapshot.DatePlayed,
            Color = PlayColor,
            Fields = BuildPlayerFields(snapshot).ToArray(),
            Footer = new EmbedFooterProperties { Text = BuildFooter(snapshot) },
        };
    }

    private static string BuildDescription(PlaySnapshot snapshot)
    {
        var items = new List<string>();

        if (!string.IsNullOrWhiteSpace(snapshot.LocationName))
            items.Add(snapshot.LocationName);

        if (snapshot.Rounds is > 0)
            items.Add(snapshot.Rounds == 1 ? "1 Round" : $"{snapshot.Rounds} Rounds");

        items.Add(FormatDuration(snapshot.Duration));

        return string.Join(" - ", items);
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        if (duration is null or { TotalMinutes: 0 })
            return "Untimed";

        var hours = duration.Value.Hours switch
        {
            0 => (string?)null,
            1 => "1 hour",
            var h => $"{h} hours"
        };

        var minutes = duration.Value.Minutes switch
        {
            0 => (string?)null,
            1 => "1 minute",
            var m => $"{m} minutes"
        };

        return (hours, minutes) switch
        {
            (not null, not null) => $"{hours} and {minutes}",
            (not null, null) => hours,
            (null, not null) => minutes,
            _ => "Untimed"
        };
    }

    private static IEnumerable<EmbedFieldProperties> BuildPlayerFields(PlaySnapshot snapshot)
    {
        var hasTeams = snapshot.Players.Any(p => !string.IsNullOrEmpty(p.Team));
        return hasTeams ? BuildTeamFields(snapshot.Players) : BuildIndividualFields(snapshot.Players);
    }

    private static IEnumerable<EmbedFieldProperties> BuildTeamFields(List<PlayerSnapshot> players)
    {
        var teams = players.GroupBy(p => p.Team ?? "").OrderBy(g => g.Key);

        foreach (var team in teams)
        {
            var teamName = int.TryParse(team.Key, out var parsed)
                ? $"Team {parsed + 1}"
                : string.IsNullOrEmpty(team.Key) ? "Unassigned" : team.Key;

            var hasWinner = team.Any(p => p.Winner);
            var title = $"\r\n{teamName}{(hasWinner ? " :trophy:" : "")}";

            var sb = new StringBuilder();
            foreach (var player in team)
            {
                sb.Append(player.PlayerName);
                AppendScore(sb, player);
                sb.AppendLine();
                if (!string.IsNullOrEmpty(player.Role))
                    sb.AppendLine($"```Role: {player.Role}```");
            }

            yield return new EmbedFieldProperties
            {
                Name = title,
                Value = sb.ToString(),
                Inline = false,
            };
        }
    }

    private static IEnumerable<EmbedFieldProperties> BuildIndividualFields(List<PlayerSnapshot> players)
    {
        var sb = new StringBuilder();

        foreach (var player in players)
        {
            sb.Append(player.PlayerName);
            AppendScore(sb, player);
            if (player.Winner) sb.Append(" :trophy:");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(player.Role))
                sb.AppendLine($"```Role: {player.Role}```");
        }

        yield return new EmbedFieldProperties
        {
            Name = "Players",
            Value = sb.ToString(),
            Inline = false,
        };
    }

    private static void AppendScore(StringBuilder sb, PlayerSnapshot player)
    {
        if (player.CalculatedScore is { } calcScore and not 0)
        {
            sb.Append($" - {calcScore}");
        }
        else if (!string.IsNullOrEmpty(player.Score))
        {
            sb.Append($" - {player.Score}");
        }
    }

    private static string BuildFooter(PlaySnapshot snapshot)
    {
        var items = new List<string>();

        if (snapshot.BggGameId is { } bggId)
            items.Add($"BGG #{bggId}");

        items.Add($"{snapshot.Players.Count} Players");
        items.Add("Right-click \u2192 Apps \u2192 Claim Player");

        return string.Join(" | ", items);
    }
}
