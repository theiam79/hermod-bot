using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wolverine;

namespace Hermod.Bot.Modules;

[SlashCommand("sharing", "Configure play sharing for this server",
    DefaultGuildPermissions = Permissions.ManageGuild)]
public class SharingModule(IServiceScopeFactory scopeFactory) : ApplicationCommandModule<SlashCommandContext>
{
    [SubSlashCommand("enable", "Enable play sharing to a channel")]
    public async Task EnableAsync(
        [SlashCommandParameter(Name = "channel", Description = "The text channel for play posts")]
        TextGuildChannel channel)
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
            await FollowupAsync(new() { Content = "This server hasn't been registered yet. Please wait a moment and try again.", Flags = MessageFlags.Ephemeral });
            return;
        }

        mapping.PostChannelId = channel.Id;
        await db.SaveChangesAsync();

        await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, true));

        await FollowupAsync(new() { Content = $"Play sharing enabled in <#{channel.Id}>.", Flags = MessageFlags.Ephemeral });
    }

    [SubSlashCommand("disable", "Disable play sharing")]
    public async Task DisableAsync()
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
            await FollowupAsync(new() { Content = "This server hasn't been registered yet. Please wait a moment and try again.", Flags = MessageFlags.Ephemeral });
            return;
        }

        mapping.PostChannelId = null;
        await db.SaveChangesAsync();

        await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, false));

        await FollowupAsync(new() { Content = "Play sharing has been disabled.", Flags = MessageFlags.Ephemeral });
    }
}
