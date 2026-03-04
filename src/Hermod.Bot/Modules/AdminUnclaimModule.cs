using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Wolverine;

namespace Hermod.Bot.Modules;

public class AdminUnclaimSelectionModule(IServiceScopeFactory scopeFactory) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("admin-unclaim")]
    public async Task HandleAdminUnclaimSelectionAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var customId = Context.Interaction.Data.CustomId;
        var colonIndex = customId.IndexOf(':');
        if (colonIndex < 0 || !Guid.TryParse(customId[(colonIndex + 1)..], out var hermodUserId))
        {
            await FollowupAsync(new() { Content = "Invalid reference.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var selectedUuid = Context.Interaction.Data.SelectedValues[0];

        await using var scope = scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var claims = await bus.InvokeAsync<GetUserClaimsResult>(
            new GetUserClaims(hermodUserId),
            timeout: TimeSpan.FromSeconds(10));

        var claim = claims.Claims.FirstOrDefault(c => c.BgStatsPlayerUuid == selectedUuid);
        var playerName = claim?.PlayerName ?? "this player";

        var confirmButton = new ButtonProperties(
            $"admin-unclaim-confirm:{hermodUserId}:{selectedUuid}",
            "Yes, unlink",
            ButtonStyle.Danger);
        var cancelButton = new ButtonProperties("admin-unclaim-cancel", "Cancel", ButtonStyle.Secondary);

        await FollowupAsync(new()
        {
            Content = $"Are you sure you want to remove the claim on **{playerName}**?",
            Components = [new ActionRowProperties([confirmButton, cancelButton])],
            Flags = MessageFlags.Ephemeral,
        });
    }
}

public class AdminUnclaimConfirmModule(IServiceScopeFactory scopeFactory) : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("admin-unclaim-confirm")]
    public async Task HandleConfirmAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var customId = Context.Interaction.Data.CustomId;
        // Format: admin-unclaim-confirm:{hermodUserId}:{bgStatsPlayerUuid}
        var afterPrefix = customId["admin-unclaim-confirm:".Length..];
        var separatorIndex = afterPrefix.IndexOf(':');
        if (separatorIndex < 0
            || !Guid.TryParse(afterPrefix[..separatorIndex], out var hermodUserId))
        {
            await FollowupAsync(new() { Content = "Invalid unclaim reference.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var bgStatsPlayerUuid = afterPrefix[(separatorIndex + 1)..];

        await using var scope = scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.InvokeAsync<UnclaimPlayerResult>(
            new UnclaimPlayer(hermodUserId, bgStatsPlayerUuid),
            timeout: TimeSpan.FromSeconds(10));

        var response = result.Status switch
        {
            UnclaimStatus.Removed => "Player claim removed successfully.",
            UnclaimStatus.NotFound => "That player claim was not found.",
            UnclaimStatus.Forbidden => "You can only remove claims for users in your server.",
            _ => "Something went wrong. Please try again later.",
        };

        await FollowupAsync(new() { Content = response, Flags = MessageFlags.Ephemeral });
    }

    [ComponentInteraction("admin-unclaim-cancel")]
    public async Task HandleCancelAsync()
    {
        await RespondAsync(InteractionCallback.Message(new()
        {
            Content = "Admin unclaim cancelled.",
            Flags = MessageFlags.Ephemeral,
        }));
    }
}
