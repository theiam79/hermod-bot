using System.Net;
using System.Text.Json;
using Hermod.Bot.Data;
using Hermod.Bot.Embeds;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Rest;

namespace Hermod.Bot.Handlers;

public static class SharePlayToGroupHandler
{
    public static async Task Handle(
        SharePlayToGroup message,
        BotDbContext db,
        IServiceProvider services,
        RestClient rest,
        ILogger logger)
    {
        var mapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.GroupId == message.GroupId && m.IsActive);

        if (mapping is null)
        {
            logger.LogWarning("No active guild mapping found for group {GroupId}", message.GroupId);
            return;
        }

        if (mapping.PostChannelId is null)
        {
            logger.LogDebug("Guild {GuildId} has no post channel configured, skipping", mapping.DiscordGuildId);
            return;
        }

        // GatewayClient is only registered in production mode (not Testing).
        // When available, validate guild/channel existence via the gateway cache.
        var gateway = services.GetService<GatewayClient>();
        string? guildName = null;
        if (gateway is not null)
        {
            if (!gateway.Cache.Guilds.TryGetValue(mapping.DiscordGuildId, out var guild))
            {
                logger.LogWarning("Discord guild {GuildId} not found in cache", mapping.DiscordGuildId);
                return;
            }

            if (!guild.Channels.TryGetValue(mapping.PostChannelId.Value, out _))
            {
                logger.LogWarning("Text channel {ChannelId} not found in guild {GuildId}",
                    mapping.PostChannelId.Value, mapping.DiscordGuildId);
                return;
            }

            guildName = guild.Name;
        }

        var channelId = mapping.PostChannelId.Value;

        // Resolve MappedUserId → Discord user IDs for mention rendering
        var mappedUserIds = message.Snapshot.Players
            .Where(p => p.MappedUserId.HasValue)
            .Select(p => p.MappedUserId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<Guid, ulong>? discordUserIds = null;
        if (mappedUserIds.Count > 0)
        {
            discordUserIds = await db.DiscordUserMappings
                .Where(m => mappedUserIds.Contains(m.HermodUserId))
                .ToDictionaryAsync(m => m.HermodUserId, m => m.DiscordUserId);
        }

        var embed = PlayEmbedBuilder.Build(message.Snapshot, discordUserIds);

        var existingPost = await db.PlayPosts
            .FirstOrDefaultAsync(p => p.GroupId == message.GroupId && p.PlayId == message.PlayId);

        if (existingPost is not null && message.ChangeType == PlayChangeType.Updated)
        {
            try
            {
                await rest.ModifyMessageAsync(channelId, existingPost.DiscordMessageId, m =>
                {
                    m.Embeds = [embed];
                });
                existingPost.PlayersJson = JsonSerializer.Serialize(message.Snapshot.Players);
                existingPost.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                logger.LogInformation("Updated play post for play {PlayId} in guild {GuildName}",
                    message.PlayId, guildName ?? mapping.DiscordGuildId.ToString());
                return;
            }
            catch (RestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("Original message {MessageId} was deleted, posting new",
                    existingPost.DiscordMessageId);
            }
        }

        try
        {
            var sentMessage = await rest.SendMessageAsync(channelId, new MessageProperties
            {
                Embeds = [embed],
            });

            var playersJson = JsonSerializer.Serialize(message.Snapshot.Players);

            if (existingPost is not null)
            {
                existingPost.DiscordChannelId = channelId;
                existingPost.DiscordMessageId = sentMessage.Id;
                existingPost.PlayersJson = playersJson;
                existingPost.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.PlayPosts.Add(new PlayPostEntity
                {
                    Id = Guid.NewGuid(),
                    GroupId = message.GroupId,
                    PlayId = message.PlayId,
                    DiscordChannelId = channelId,
                    DiscordMessageId = sentMessage.Id,
                    PlayersJson = playersJson,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Posted play {PlayId} ({GameName}) to channel {ChannelId} in {GuildName}",
                message.PlayId, message.Snapshot.GameName, channelId,
                guildName ?? mapping.DiscordGuildId.ToString());
        }
        catch (RestException ex)
        {
            logger.LogError(ex, "Failed to send play embed to channel {ChannelId} in guild {GuildId}",
                mapping.PostChannelId.Value, mapping.DiscordGuildId);
        }
    }
}
