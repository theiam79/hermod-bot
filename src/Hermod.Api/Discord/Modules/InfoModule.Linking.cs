using System.Text;
using Discord;
using Discord.Interactions;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Discord.Modules;

public partial class InfoModule
{
    [SlashCommand("my-players", "View your linked BGStats players")]
    public async Task MyPlayersAsync()
    {
        var discordId = Context.User.Id.ToString();
        const string provider = "Discord";

        var login = await _db.UserExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == discordId);

        if (login is null)
        {
            await RespondAsync("You have no player links.", ephemeral: true);
            return;
        }

        var mappings = await _db.PlayerMappings
            .Where(pm => pm.MappedUserId == login.UserId)
            .ToListAsync();

        if (mappings.Count == 0)
        {
            await RespondAsync("You have no player links.", ephemeral: true);
            return;
        }

        // For each mapping, find the most recent player name
        var uuids = mappings.Select(m => m.BgStatsPlayerUuid).ToList();
        var playerNames = await _db.PlayPlayers
            .Where(pp => uuids.Contains(pp.BgStatsPlayerUuid))
            .GroupBy(pp => pp.BgStatsPlayerUuid)
            .Select(g => new
            {
                Uuid = g.Key,
                Name = g.OrderByDescending(pp => pp.Play.DatePlayed).First().PlayerName,
            })
            .ToListAsync();

        var nameLookup = playerNames.ToDictionary(p => p.Uuid, p => p.Name);

        var sb = new StringBuilder("**Your linked players:**\n");
        foreach (var mapping in mappings)
        {
            var name = nameLookup.GetValueOrDefault(mapping.BgStatsPlayerUuid, "Unknown");
            sb.AppendLine($"- **{name}** (`{mapping.BgStatsPlayerUuid}`)");
        }

        var menu = new SelectMenuBuilder()
            .WithCustomId("unlink-player")
            .WithPlaceholder("Select a player to unlink")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var mapping in mappings.Take(25))
        {
            var name = nameLookup.GetValueOrDefault(mapping.BgStatsPlayerUuid, "Unknown");
            menu.AddOption(name, mapping.Id.Value.ToString(), mapping.BgStatsPlayerUuid);
        }

        var component = new ComponentBuilder().WithSelectMenu(menu).Build();
        await RespondAsync(sb.ToString(), components: component, ephemeral: true);
    }

    [ComponentInteraction("unlink-player")]
    public async Task HandleUnlinkSelectionAsync(string[] selectedValues)
    {
        if (selectedValues.Length == 0) return;

        if (!Guid.TryParse(selectedValues[0], out var mappingIdGuid))
        {
            await RespondAsync("Invalid selection.", ephemeral: true);
            return;
        }

        var discordId = Context.User.Id.ToString();
        const string provider = "Discord";

        var login = await _db.UserExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == discordId);

        if (login is null)
        {
            await RespondAsync("You have no player links.", ephemeral: true);
            return;
        }

        var mappingId = PlayerMappingId.From(mappingIdGuid);
        var mapping = await _db.PlayerMappings
            .FirstOrDefaultAsync(pm => pm.Id == mappingId && pm.MappedUserId == login.UserId);

        if (mapping is null)
        {
            await RespondAsync("Player link not found.", ephemeral: true);
            return;
        }

        // Get the player name for the response before deleting
        var playerName = await _db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == mapping.BgStatsPlayerUuid)
            .OrderByDescending(pp => pp.Play.DatePlayed)
            .Select(pp => pp.PlayerName)
            .FirstOrDefaultAsync() ?? "Unknown";

        _db.PlayerMappings.Remove(mapping);
        await _db.SaveChangesAsync();

        await RespondAsync($"Unlinked from **{playerName}**.", ephemeral: true);
    }
}
