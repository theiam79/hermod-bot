using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Discord.Modules;

public class ClaimPlayerModule(HermodContext db) : InteractionModuleBase<SocketInteractionContext>
{
    private readonly HermodContext _db = db;

    [MessageCommand("Claim Player")]
    public async Task ClaimPlayerAsync(IMessage message)
    {
        var messageId = message.Id;

        var post = await _db.PlayPosts
            .Include(pp => pp.Play)
                .ThenInclude(p => p.Players)
            .FirstOrDefaultAsync(pp => pp.DiscordMessageId == messageId);

        if (post is null)
        {
            await RespondAsync("This message is not a Hermod play embed.", ephemeral: true);
            return;
        }

        var players = post.Play.Players
            .OrderBy(p => p.Rank ?? int.MaxValue)
            .ThenBy(p => p.PlayerName)
            .Take(25)
            .ToList();

        if (players.Count == 0)
        {
            await RespondAsync("No players found for this play.", ephemeral: true);
            return;
        }

        var menu = new SelectMenuBuilder()
            .WithCustomId($"claim-player:{post.PlayId.Value}")
            .WithPlaceholder("Select yourself")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var player in players)
        {
            var description = player.Score is not null ? $"Score: {player.Score}" : null;
            menu.AddOption(player.PlayerName, player.BgStatsPlayerUuid, description);
        }

        var component = new ComponentBuilder().WithSelectMenu(menu).Build();
        await RespondAsync("Which player are you?", components: component, ephemeral: true);
    }

    [ComponentInteraction("claim-player:*")]
    public async Task HandleClaimSelectionAsync(string playIdStr, string[] selectedValues)
    {
        if (selectedValues.Length == 0) return;

        var selectedUuid = selectedValues[0];
        var claimingDiscordId = Context.User.Id.ToString();

        var claimingUserId = await FindOrCreateUserAsync(claimingDiscordId);

        // Check-then-insert PlayerMapping
        var exists = await _db.PlayerMappings
            .AnyAsync(pm => pm.BgStatsPlayerUuid == selectedUuid && pm.MappedUserId == claimingUserId);

        if (!exists)
        {
            _db.PlayerMappings.Add(new PlayerMappingEntity
            {
                Id = PlayerMappingId.From(Guid.NewGuid()),
                BgStatsPlayerUuid = selectedUuid,
                MappedUserId = claimingUserId,
            });
        }

        // Set MappedUserId on the specific play player
        if (Guid.TryParse(playIdStr, out var playIdGuid))
        {
            var playId = PlayId.From(playIdGuid);
            var playPlayer = await _db.PlayPlayers
                .FirstOrDefaultAsync(pp => pp.PlayId == playId && pp.BgStatsPlayerUuid == selectedUuid);

            if (playPlayer is not null && playPlayer.MappedUserId is null)
            {
                playPlayer.MappedUserId = claimingUserId;
            }
        }

        // Backfill: update all other PlayPlayerEntity rows matching this UUID where MappedUserId is null
        var unmappedPlayers = await _db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == selectedUuid && pp.MappedUserId == null)
            .ToListAsync();

        foreach (var pp in unmappedPlayers)
        {
            pp.MappedUserId = claimingUserId;
        }

        // Get the player name for the response
        var playerName = await _db.PlayPlayers
            .Where(pp => pp.BgStatsPlayerUuid == selectedUuid)
            .Select(pp => pp.PlayerName)
            .FirstOrDefaultAsync() ?? "Unknown";

        await _db.SaveChangesAsync();

        await RespondAsync($"You've been linked as **{playerName}**.", ephemeral: true);
    }

    private async Task<UserId> FindOrCreateUserAsync(string discordId)
    {
        const string provider = "Discord";

        var login = await _db.UserExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == discordId);

        if (login is not null)
        {
            return login.UserId;
        }

        var user = new UserEntity
        {
            Id = UserId.From(Guid.NewGuid()),
            DisplayName = $"Discord:{discordId}",
        };

        _db.Users.Add(user);

        _db.UserExternalLogins.Add(new UserExternalLoginEntity
        {
            Id = ExternalLoginId.From(Guid.NewGuid()),
            UserId = user.Id,
            Provider = provider,
            ProviderKey = discordId,
        });

        return user.Id;
    }
}
