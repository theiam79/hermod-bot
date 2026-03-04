using System.Text.Json;
using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Hermod.Bot.Modules;

public class ClaimPlayerButtonModule(IServiceScopeFactory scopeFactory) : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("claim-player-btn")]
    public async Task HandleClaimButtonAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var customId = Context.Interaction.Data.CustomId;
        var colonIndex = customId.IndexOf(':');
        if (colonIndex < 0 || !Guid.TryParse(customId[(colonIndex + 1)..], out var playId))
        {
            await FollowupAsync(new() { Content = "Invalid play reference.", Flags = MessageFlags.Ephemeral });
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        var post = await db.PlayPosts
            .FirstOrDefaultAsync(p => p.DiscordMessageId == Context.Interaction.Message.Id);

        if (post is null)
        {
            await FollowupAsync(new() { Content = "This play embed was not found.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var (options, errorMessage) = ClaimPlayerHelper.BuildClaimableOptions(post);
        if (options is null)
        {
            await FollowupAsync(new() { Content = errorMessage!, Flags = MessageFlags.Ephemeral });
            return;
        }

        var menu = new StringMenuProperties($"claim-player:{playId}", options)
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
