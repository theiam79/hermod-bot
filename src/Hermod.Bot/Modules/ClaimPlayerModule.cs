using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using Wolverine;

namespace Hermod.Bot.Modules;

public class ClaimPlayerCommandModule(IServiceScopeFactory scopeFactory) : ApplicationCommandModule<MessageCommandContext>
{
    [MessageCommand("Claim Player")]
    public async Task ClaimPlayerAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var message = Context.Interaction.Data.TargetMessage;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        var post = await db.PlayPosts
            .FirstOrDefaultAsync(p => p.DiscordMessageId == message.Id);

        if (post is null)
        {
            await FollowupAsync(new() { Content = "This isn't a Hermod play embed.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var (options, errorMessage) = ClaimPlayerHelper.BuildClaimableOptions(post);
        if (options is null)
        {
            await FollowupAsync(new() { Content = errorMessage!, Flags = MessageFlags.Ephemeral });
            return;
        }

        var menu = new StringMenuProperties($"claim-player:{post.PlayId}", options)
        {
            Placeholder = "Select your player",
            MinValues = 1,
            MaxValues = 1,
        };

        await FollowupAsync(new()
        {
            Content = "Which player are you?",
            Components = [menu],
            Flags = MessageFlags.Ephemeral,
        });
    }
}

public class ClaimPlayerSelectionModule(IServiceScopeFactory scopeFactory) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("claim-player")]
    public async Task HandleClaimSelectionAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        // Extract play ID from the custom ID: "claim-player:{playId}"
        var customId = Context.Interaction.Data.CustomId;
        var colonIndex = customId.IndexOf(':');
        if (colonIndex < 0 || !Guid.TryParse(customId[(colonIndex + 1)..], out var playId))
        {
            await FollowupAsync(new() { Content = "Invalid play reference.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var selectedUuid = Context.Interaction.Data.SelectedValues[0];
        var discordId = Context.Interaction.User.Id.ToString();

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.InvokeAsync<ClaimPlayerResult>(
            new ClaimPlayer(Providers.Discord, discordId, selectedUuid, playId),
            timeout: TimeSpan.FromSeconds(10));

        if (result.UserId.HasValue)
            await DiscordUserMappingHelper.UpsertAsync(db, Context.Interaction.User.Id, result.UserId.Value);

        var response = result.Status switch
        {
            ClaimPlayerStatus.Claimed => $"You've been linked to **{result.PlayerName}**! Future plays will recognize you automatically.",
            ClaimPlayerStatus.AlreadyClaimed => $"This player is already claimed by **{result.ClaimantDisplayName}**.",
            ClaimPlayerStatus.IsUploader => "You uploaded this play — your player was linked automatically.",
            ClaimPlayerStatus.NotRegistered => "You need to register first. Use `/enroll` to get started.",
            ClaimPlayerStatus.PlayerNotFound => "That player wasn't found in the system.",
            _ => "Something went wrong. Please try again later.",
        };

        await FollowupAsync(new() { Content = response, Flags = MessageFlags.Ephemeral });
    }
}
