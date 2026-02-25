using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Bot.Modules;

public class ClaimPlayerModule(IServiceScopeFactory scopeFactory) : InteractionModuleBase<SocketInteractionContext>
{
    [MessageCommand("Claim Player")]
    public async Task ClaimPlayerAsync(global::Discord.IMessage message)
    {
        await DeferAsync(ephemeral: true);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        var post = await db.PlayPosts
            .FirstOrDefaultAsync(p => p.DiscordMessageId == message.Id);

        if (post is null)
        {
            await FollowupAsync("This isn't a Hermod play embed.", ephemeral: true);
            return;
        }

        if (string.IsNullOrEmpty(post.PlayersJson))
        {
            await FollowupAsync("This play has no player data. Try re-uploading.", ephemeral: true);
            return;
        }

        var players = JsonSerializer.Deserialize<List<PlayerSnapshot>>(post.PlayersJson);
        if (players is null or { Count: 0 })
        {
            await FollowupAsync("No players found in this play.", ephemeral: true);
            return;
        }

        var claimable = players.Where(p => p.MappedUserId is null).ToList();
        if (claimable.Count == 0)
        {
            await FollowupAsync("All players in this play have already been linked.", ephemeral: true);
            return;
        }

        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId($"claim-player:{post.PlayId}")
            .WithPlaceholder("Select your player")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var player in claimable.Take(25))
        {
            var description = player.CalculatedScore is { } score and not 0
                ? $"Score: {score}"
                : !string.IsNullOrEmpty(player.Score)
                    ? $"Score: {player.Score}"
                    : null;

            menuBuilder.AddOption(player.PlayerName, player.BgStatsPlayerUuid, description);
        }

        var component = new ComponentBuilder()
            .WithSelectMenu(menuBuilder)
            .Build();

        await FollowupAsync("Which player are you?", components: component, ephemeral: true);
    }

    [ComponentInteraction("claim-player:*")]
    public async Task HandleClaimSelectionAsync(string playIdStr, string[] selectedValues)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(playIdStr, out var playId))
        {
            await FollowupAsync("Invalid play reference.", ephemeral: true);
            return;
        }

        var selectedUuid = selectedValues[0];
        var discordId = Context.User.Id.ToString();

        await using var scope = scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.InvokeAsync<ClaimPlayerResult>(
            new ClaimPlayer(discordId, selectedUuid, playId),
            timeout: TimeSpan.FromSeconds(10));

        var response = result.Status switch
        {
            ClaimPlayerStatus.Claimed => $"You've been linked to **{result.PlayerName}**! Future plays will recognize you automatically.",
            ClaimPlayerStatus.AlreadyClaimed => "You've already claimed this player.",
            ClaimPlayerStatus.IsUploader => "You uploaded this play — your player was linked automatically.",
            ClaimPlayerStatus.NotRegistered => "You need to register first. Use `/enroll` to get started.",
            ClaimPlayerStatus.PlayerNotFound => "That player wasn't found in the system.",
            _ => "Something went wrong. Please try again later.",
        };

        await FollowupAsync(response, ephemeral: true);
    }
}
