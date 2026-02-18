# Task 02 — GroupAdmin Slash Commands

## Why
Server admins need a way to configure Hermod without touching a database. Three slash commands
cover the essential setup: opt-in to sharing, designate the post channel, and tune the
multi-play threshold. All are ephemeral so the configuration details stay between the admin
and the bot.

## Steps

### Create `src/Hermod.Api/Discord/Modules/GroupAdminModule.cs`

```csharp
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Discord.Modules;

[Group("hermod", "Hermod bot commands")]
public class GroupAdminModule(HermodContext db) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("allow-sharing", "Enable or disable play sharing for this server")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task AllowSharingAsync(bool enabled)
    {
        var group = await db.Groups
            .FirstOrDefaultAsync(g => g.DiscordGuildId == Context.Guild.Id);

        if (group is null)
        {
            await RespondAsync("This server is not registered with Hermod.", ephemeral: true);
            return;
        }

        group.AllowSharing = enabled;
        await db.SaveChangesAsync();

        await RespondAsync(
            $"Play sharing is now **{(enabled ? "enabled" : "disabled")}** for this server.",
            ephemeral: true);
    }

    [SlashCommand("set-channel", "Set the channel where play embeds will be posted")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetChannelAsync(ITextChannel channel)
    {
        var group = await db.Groups
            .FirstOrDefaultAsync(g => g.DiscordGuildId == Context.Guild.Id);

        if (group is null)
        {
            await RespondAsync("This server is not registered with Hermod.", ephemeral: true);
            return;
        }

        group.DiscordPostChannelId = channel.Id;
        await db.SaveChangesAsync();

        await RespondAsync(
            $"Play embeds will be posted to {channel.Mention}.",
            ephemeral: true);
    }

    [SlashCommand("set-threshold", "Number of plays per upload at/above which a summary embed is posted")]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    public async Task SetThresholdAsync([MinValue(1)] [MaxValue(50)] int threshold)
    {
        var group = await db.Groups
            .FirstOrDefaultAsync(g => g.DiscordGuildId == Context.Guild.Id);

        if (group is null)
        {
            await RespondAsync("This server is not registered with Hermod.", ephemeral: true);
            return;
        }

        group.SpamThreshold = threshold;
        await db.SaveChangesAsync();

        await RespondAsync(
            $"Uploads with **{threshold}** or more plays will be posted as a single summary embed.",
            ephemeral: true);
    }
}
```

## Notes
- `[DefaultMemberPermissions(GuildPermission.ManageGuild)]` is set per-command rather than
  at the class level so that other commands in the `hermod` group (e.g. `/hermod ping`) are
  not affected. Discord applies the permission at the command level.
- `HermodContext` is injected via the constructor. Discord.Addons.Hosting resolves module
  constructors through the DI container, so scoped services work here — each interaction
  execution gets a fresh scope.
- `[MinValue(1)] [MaxValue(50)]` on the threshold parameter provides Discord-side validation
  so invalid values are rejected before reaching the handler.
- The `"This server is not registered"` error should not occur in normal operation (GuildHandler
  creates rows on startup), but is a useful diagnostic if something goes wrong.
- All responses are `ephemeral: true` — only the invoking admin sees the confirmation.

## Acceptance Criteria
- [ ] `src/Hermod.Api/Discord/Modules/GroupAdminModule.cs` exists
- [ ] `dotnet build src/Hermod.Api/Hermod.Api.csproj` succeeds
- [ ] `/hermod allow-sharing true` enables sharing and responds ephemerally
- [ ] `/hermod allow-sharing false` disables sharing and responds ephemerally
- [ ] `/hermod set-channel #channel` sets the post channel and responds with a mention
- [ ] `/hermod set-threshold 5` sets the threshold to 5 and responds ephemerally
- [ ] A user without Manage Guild cannot invoke any of the three commands
- [ ] `/hermod ping` (from InfoModule) still works and does not require Manage Guild
