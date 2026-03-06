using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wolverine;

namespace Hermod.Bot.Modules;

public class LeaveModule(IServiceScopeFactory scopeFactory) : ApplicationCommandModule<SlashCommandContext>
{
    [SlashCommand("leave", "Leave this server's play-sharing group")]
    public async Task LeaveAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var guildId = Context.Interaction.GuildId;
        if (guildId is null)
        {
            await FollowupAsync(new() { Content = "This command can only be used in a server.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var mapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == guildId.Value && m.IsActive);
        if (mapping is null)
        {
            await FollowupAsync(new() { Content = "This server doesn't have a play-sharing group yet.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var discordId = Context.Interaction.User.Id.ToString();

        var result = await bus.InvokeAsync<LeaveGroupResult>(
            new LeaveGroup(Providers.Discord, discordId, mapping.GroupId),
            timeout: TimeSpan.FromSeconds(10));

        var message = result.Status switch
        {
            LeaveGroupStatus.Left => "You've left the play-sharing group for this server.",
            LeaveGroupStatus.NotMember => "You're not a member of this server's play-sharing group.",
            LeaveGroupStatus.GroupNotFound => "This server doesn't have a play-sharing group yet.",
            LeaveGroupStatus.NotRegistered => "You need to register first. Use `/enroll` to get started.",
            _ => "Something went wrong. Please try again later.",
        };

        await FollowupAsync(new() { Content = message, Flags = MessageFlags.Ephemeral });
    }
}
