using System.Net;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Hermod.Bot.Data;
using Hermod.Bot.Embeds;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hermod.Bot.Handlers;

public static class SharePlayToGroupHandler
{
    public static async Task Handle(
        SharePlayToGroup message,
        BotDbContext db,
        DiscordSocketClient discord,
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

        var guild = discord.GetGuild(mapping.DiscordGuildId);
        if (guild is null)
        {
            logger.LogWarning("Discord guild {GuildId} not found in cache", mapping.DiscordGuildId);
            return;
        }

        var channel = guild.GetTextChannel(mapping.PostChannelId.Value);
        if (channel is null)
        {
            logger.LogWarning("Text channel {ChannelId} not found in guild {GuildId}",
                mapping.PostChannelId.Value, mapping.DiscordGuildId);
            return;
        }

        var embed = PlayEmbedBuilder.Build(message.Snapshot);

        var existingPost = await db.PlayPosts
            .FirstOrDefaultAsync(p => p.GroupId == message.GroupId && p.PlayId == message.PlayId);

        if (existingPost is not null && message.ChangeType == PlayChangeType.Updated)
        {
            try
            {
                var existingMessage = await channel.GetMessageAsync(existingPost.DiscordMessageId);
                if (existingMessage is IUserMessage userMessage)
                {
                    await userMessage.ModifyAsync(m => m.Embed = embed);
                    existingPost.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    logger.LogInformation("Updated play post for play {PlayId} in guild {GuildName}",
                        message.PlayId, guild.Name);
                    return;
                }

                logger.LogWarning("Original message {MessageId} not found or not editable, posting new",
                    existingPost.DiscordMessageId);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("Original message {MessageId} was deleted, posting new",
                    existingPost.DiscordMessageId);
            }
        }

        try
        {
            var sentMessage = await channel.SendMessageAsync(embed: embed);

            if (existingPost is not null)
            {
                existingPost.DiscordChannelId = channel.Id;
                existingPost.DiscordMessageId = sentMessage.Id;
                existingPost.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.PlayPosts.Add(new PlayPostEntity
                {
                    Id = Guid.NewGuid(),
                    GroupId = message.GroupId,
                    PlayId = message.PlayId,
                    DiscordChannelId = channel.Id,
                    DiscordMessageId = sentMessage.Id,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Posted play {PlayId} ({GameName}) to #{ChannelName} in {GuildName}",
                message.PlayId, message.Snapshot.GameName, channel.Name, guild.Name);
        }
        catch (HttpException ex)
        {
            logger.LogError(ex, "Failed to send play embed to channel {ChannelId} in guild {GuildId}",
                mapping.PostChannelId.Value, mapping.DiscordGuildId);
        }
    }
}
