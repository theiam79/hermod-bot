using System.Text.Json;
using Hermod.Bot.Data;
using Hermod.Messages;
using NetCord.Rest;

namespace Hermod.Bot.Modules;

public static class ClaimPlayerHelper
{
    /// <summary>
    /// Parses the PlayPost's PlayersJson and builds menu options for unclaimed players.
    /// Returns (options, null) on success, or (null, errorMessage) on failure.
    /// </summary>
    public static (StringMenuSelectOptionProperties[]? Options, string? ErrorMessage) BuildClaimableOptions(PlayPostEntity post)
    {
        if (string.IsNullOrEmpty(post.PlayersJson))
            return (null, "This play has no player data. Try re-uploading.");

        var players = JsonSerializer.Deserialize<List<PlayerSnapshot>>(post.PlayersJson);
        if (players is null or { Count: 0 })
            return (null, "No players found in this play.");

        var claimable = players.Where(p => p.MappedUserId is null).ToList();
        if (claimable.Count == 0)
            return (null, "All players in this play have already been linked.");

        var options = claimable.Take(25).Select(player =>
        {
            var description = player.CalculatedScore is { } score and not 0
                ? $"Score: {score}"
                : !string.IsNullOrEmpty(player.Score)
                    ? $"Score: {player.Score}"
                    : null;

            return new StringMenuSelectOptionProperties(player.PlayerName, player.BgStatsPlayerUuid)
            {
                Description = description,
            };
        }).ToArray();

        return (options, null);
    }
}
