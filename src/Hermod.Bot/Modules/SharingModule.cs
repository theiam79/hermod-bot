using System.Text.Json;
using Hermod.Bot.Data;
using Hermod.Bot.Services;
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

    [SubSlashCommand("activate", "Activate this server's Hermod registration")]
    public async Task ActivateAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var registrationService = scope.ServiceProvider.GetRequiredService<GuildRegistrationService>();

        var guildId = Context.Interaction.GuildId;
        if (guildId is null)
        {
            await FollowupAsync(new() { Content = "This command can only be used in a server.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var mapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == guildId.Value);

        if (mapping is null)
        {
            var guildName = Context.Client.Cache.Guilds.GetValueOrDefault(guildId.Value)?.Name ?? "Unknown Server";
            await registrationService.RegisterGuildAsync(guildId.Value, guildName);
            await FollowupAsync(new() { Content = "Server has been registered and activated.", Flags = MessageFlags.Ephemeral });
            return;
        }

        if (mapping.IsActive)
        {
            await FollowupAsync(new() { Content = "This server is already active.", Flags = MessageFlags.Ephemeral });
            return;
        }

        await registrationService.ReactivateAsync(mapping);
        await FollowupAsync(new() { Content = "Server has been reactivated.", Flags = MessageFlags.Ephemeral });
    }

    [SubSlashCommand("deactivate", "Deactivate this server's Hermod registration")]
    public async Task DeactivateAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var registrationService = scope.ServiceProvider.GetRequiredService<GuildRegistrationService>();

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
            await FollowupAsync(new() { Content = "This server is not currently active.", Flags = MessageFlags.Ephemeral });
            return;
        }

        await registrationService.DeactivateAsync(mapping);
        await FollowupAsync(new() { Content = "Server has been deactivated. Use `/sharing activate` to re-enable.", Flags = MessageFlags.Ephemeral });
    }

    [SubSlashCommand("unclaim", "Remove a player claim for a user")]
    public async Task UnclaimAsync(
        [SlashCommandParameter(Name = "user", Description = "The user whose claim to remove")]
        User targetUser)
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

        var guildMapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == guildId.Value && m.IsActive);

        if (guildMapping is null)
        {
            await FollowupAsync(new() { Content = "This server hasn't been registered yet.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var userMapping = await db.DiscordUserMappings
            .FirstOrDefaultAsync(m => m.DiscordUserId == targetUser.Id);

        if (userMapping is null)
        {
            await FollowupAsync(new() { Content = "No claims found for this user in this server.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var result = await bus.InvokeAsync<GetUserClaimsResult>(
            new GetUserClaims(userMapping.HermodUserId),
            timeout: TimeSpan.FromSeconds(10));

        if (result.Claims.Count == 0)
        {
            await FollowupAsync(new() { Content = "No claims found for this user in this server.", Flags = MessageFlags.Ephemeral });
            return;
        }

        // Scope claims to this guild: only show claims for players that appear in plays posted to this guild
        var groupPlayerUuids = await GetGroupPlayerUuidsAsync(db, guildMapping.GroupId);
        var relevantClaims = result.Claims
            .Where(c => groupPlayerUuids.Contains(c.BgStatsPlayerUuid))
            .ToList();

        if (relevantClaims.Count == 0)
        {
            await FollowupAsync(new() { Content = "No claims found for this user in this server.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var options = relevantClaims.Take(25).Select(claim =>
            new StringMenuSelectOptionProperties(claim.PlayerName, claim.BgStatsPlayerUuid)
            {
                Description = claim.PlayCount == 1
                    ? "1 play"
                    : $"{claim.PlayCount} plays",
            }).ToArray();

        var menu = new StringMenuProperties($"admin-unclaim:{userMapping.HermodUserId}", options)
        {
            Placeholder = "Select a player to unclaim",
            MinValues = 1,
            MaxValues = 1,
        };

        await FollowupAsync(new()
        {
            Content = $"Which claim would you like to remove from <@{targetUser.Id}>?",
            Components = [menu],
            Flags = MessageFlags.Ephemeral,
        });
    }

    private static async Task<HashSet<string>> GetGroupPlayerUuidsAsync(BotDbContext db, Guid groupId)
    {
        var playersJsonList = await db.PlayPosts
            .Where(p => p.GroupId == groupId)
            .Select(p => p.PlayersJson)
            .ToListAsync();

        var uuids = new HashSet<string>();
        foreach (var json in playersJsonList)
        {
            if (string.IsNullOrEmpty(json)) continue;
            var players = JsonSerializer.Deserialize<List<PlayerSnapshot>>(json);
            if (players is null) continue;
            foreach (var player in players)
                uuids.Add(player.BgStatsPlayerUuid);
        }

        return uuids;
    }
}
