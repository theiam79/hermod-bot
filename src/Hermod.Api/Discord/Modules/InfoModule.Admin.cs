using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Discord.Modules;

public partial class InfoModule
{
    [SlashCommand("allow-sharing", "Enable or disable play sharing for this server")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task AllowSharingAsync(bool enabled)
    {
        var group = await _db.Groups
            .FirstOrDefaultAsync(g => g.DiscordGuildId == Context.Guild.Id);

        if (group is null)
        {
            await RespondAsync("This server is not registered with Hermod.", ephemeral: true);
            return;
        }

        group.AllowSharing = enabled;
        await _db.SaveChangesAsync();

        await RespondAsync(
            $"Play sharing is now **{(enabled ? "enabled" : "disabled")}** for this server.",
            ephemeral: true);
    }

    [SlashCommand("set-channel", "Set the channel where play embeds will be posted")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetChannelAsync(ITextChannel channel)
    {
        var group = await _db.Groups
            .FirstOrDefaultAsync(g => g.DiscordGuildId == Context.Guild.Id);

        if (group is null)
        {
            await RespondAsync("This server is not registered with Hermod.", ephemeral: true);
            return;
        }

        group.DiscordPostChannelId = channel.Id;
        await _db.SaveChangesAsync();

        await RespondAsync(
            $"Play embeds will be posted to {channel.Mention}.",
            ephemeral: true);
    }

    [SlashCommand("set-threshold", "Number of plays per upload at/above which a summary embed is posted")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetThresholdAsync([MinValue(1)] [MaxValue(50)] int threshold)
    {
        var group = await _db.Groups
            .FirstOrDefaultAsync(g => g.DiscordGuildId == Context.Guild.Id);

        if (group is null)
        {
            await RespondAsync("This server is not registered with Hermod.", ephemeral: true);
            return;
        }

        group.SpamThreshold = threshold;
        await _db.SaveChangesAsync();

        await RespondAsync(
            $"Uploads with **{threshold}** or more plays will be posted as a single summary embed.",
            ephemeral: true);
    }
}
