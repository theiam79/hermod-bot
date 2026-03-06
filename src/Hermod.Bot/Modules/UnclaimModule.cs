using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using Wolverine;

namespace Hermod.Bot.Modules;

public class UnclaimCommandModule(IServiceScopeFactory scopeFactory) : ApplicationCommandModule<SlashCommandContext>
{
    [SlashCommand("unclaim", "Remove one of your player claims")]
    public async Task UnclaimAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var userMapping = await db.DiscordUserMappings
            .FirstOrDefaultAsync(m => m.DiscordUserId == Context.Interaction.User.Id);

        if (userMapping is null)
        {
            await FollowupAsync(new()
            {
                Content = "You haven't claimed any players.",
                Flags = MessageFlags.Ephemeral,
            });
            return;
        }

        var result = await bus.InvokeAsync<GetUserClaimsResult>(
            new GetUserClaims(userMapping.HermodUserId),
            timeout: TimeSpan.FromSeconds(10));

        if (result.Claims.Count == 0)
        {
            await FollowupAsync(new()
            {
                Content = "You haven't claimed any players.",
                Flags = MessageFlags.Ephemeral,
            });
            return;
        }

        var options = result.Claims.Take(25).Select(claim =>
            new StringMenuSelectOptionProperties(claim.PlayerName, claim.BgStatsPlayerUuid)
            {
                Description = claim.PlayCount == 1
                    ? "1 play"
                    : $"{claim.PlayCount} plays",
            }).ToArray();

        var menu = new StringMenuProperties("unclaim-select", options)
        {
            Placeholder = "Select a player to unclaim",
            MinValues = 1,
            MaxValues = 1,
        };

        await FollowupAsync(new()
        {
            Content = "Which player claim would you like to remove?",
            Components = [menu],
            Flags = MessageFlags.Ephemeral,
        });
    }
}

public class UnclaimSelectionModule(IServiceScopeFactory scopeFactory) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("unclaim-select")]
    public async Task HandleUnclaimSelectionAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var selectedUuid = Context.Interaction.Data.SelectedValues[0];

        // Look up player name for the confirmation message
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var userMapping = await db.DiscordUserMappings
            .FirstOrDefaultAsync(m => m.DiscordUserId == Context.Interaction.User.Id);

        if (userMapping is null)
        {
            await FollowupAsync(new()
            {
                Content = "Could not find your user mapping.",
                Flags = MessageFlags.Ephemeral,
            });
            return;
        }

        var claims = await bus.InvokeAsync<GetUserClaimsResult>(
            new GetUserClaims(userMapping.HermodUserId),
            timeout: TimeSpan.FromSeconds(10));

        var claim = claims.Claims.FirstOrDefault(c => c.BgStatsPlayerUuid == selectedUuid);
        var playerName = claim?.PlayerName ?? "this player";

        var confirmButton = new ButtonProperties($"unclaim-confirm:{selectedUuid}", "Yes, unlink", ButtonStyle.Danger);
        var cancelButton = new ButtonProperties("unclaim-cancel", "Cancel", ButtonStyle.Secondary);

        await FollowupAsync(new()
        {
            Content = $"Are you sure you want to unlink **{playerName}**? This can't be undone automatically.",
            Components = [new ActionRowProperties([confirmButton, cancelButton])],
            Flags = MessageFlags.Ephemeral,
        });
    }
}

public class UnclaimConfirmModule(IServiceScopeFactory scopeFactory) : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("unclaim-confirm")]
    public async Task HandleConfirmAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var customId = Context.Interaction.Data.CustomId;
        var colonIndex = customId.IndexOf(':');
        if (colonIndex < 0)
        {
            await FollowupAsync(new() { Content = "Invalid unclaim reference.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var bgStatsPlayerUuid = customId[(colonIndex + 1)..];

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var userMapping = await db.DiscordUserMappings
            .FirstOrDefaultAsync(m => m.DiscordUserId == Context.Interaction.User.Id);

        if (userMapping is null)
        {
            await FollowupAsync(new() { Content = "Could not find your user mapping.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var result = await bus.InvokeAsync<UnclaimPlayerResult>(
            new UnclaimPlayer(userMapping.HermodUserId, bgStatsPlayerUuid),
            timeout: TimeSpan.FromSeconds(10));

        var response = result.Status switch
        {
            UnclaimStatus.Removed => "Player claim removed successfully.",
            UnclaimStatus.NotFound => "That player claim was not found.",
            UnclaimStatus.Forbidden => "You can only remove your own claims.",
            _ => "Something went wrong. Please try again later.",
        };

        await FollowupAsync(new() { Content = response, Flags = MessageFlags.Ephemeral });
    }

    [ComponentInteraction("unclaim-cancel")]
    public async Task HandleCancelAsync()
    {
        await RespondAsync(InteractionCallback.Message(new()
        {
            Content = "Unclaim cancelled.",
            Flags = MessageFlags.Ephemeral,
        }));
    }
}
