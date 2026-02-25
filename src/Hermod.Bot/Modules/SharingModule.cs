using Discord;
using Discord.Interactions;
using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Bot.Modules;

[Group("sharing", "Configure play sharing for this server")]
[DefaultMemberPermissions(GuildPermission.ManageGuild)]
[RequireUserPermission(GuildPermission.ManageGuild)]
public class SharingModule(IServiceScopeFactory scopeFactory) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("enable", "Enable play sharing to a channel")]
    public async Task EnableAsync(
        [Summary("channel", "The text channel for play posts")]
        ITextChannel channel)
    {
        await DeferAsync(ephemeral: true);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var guildId = Context.Guild?.Id;
        if (guildId is null)
        {
            await FollowupAsync("This command can only be used in a server.", ephemeral: true);
            return;
        }

        var mapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == guildId.Value && m.IsActive);

        if (mapping is null)
        {
            await FollowupAsync("This server hasn't been registered yet. Please wait a moment and try again.",
                ephemeral: true);
            return;
        }

        mapping.PostChannelId = channel.Id;
        await db.SaveChangesAsync();

        await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, true));

        await FollowupAsync($"Play sharing enabled in {channel.Mention}.", ephemeral: true);
    }

    [SlashCommand("disable", "Disable play sharing")]
    public async Task DisableAsync()
    {
        await DeferAsync(ephemeral: true);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var guildId = Context.Guild?.Id;
        if (guildId is null)
        {
            await FollowupAsync("This command can only be used in a server.", ephemeral: true);
            return;
        }

        var mapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == guildId.Value && m.IsActive);

        if (mapping is null)
        {
            await FollowupAsync("This server hasn't been registered yet. Please wait a moment and try again.",
                ephemeral: true);
            return;
        }

        mapping.PostChannelId = null;
        await db.SaveChangesAsync();

        await bus.PublishAsync(new UpdateGroupSharing(mapping.GroupId, false));

        await FollowupAsync("Play sharing has been disabled.", ephemeral: true);
    }
}
