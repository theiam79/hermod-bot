using System.Text;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hermod.Bot.Handlers;

public static class DistributePlayFileHandler
{
    public static async Task Handle(
        DistributePlayFile message,
        BotDbContext db,
        DiscordSocketClient discord,
        ILogger logger)
    {
        var mapping = await db.DiscordUserMappings
            .FirstOrDefaultAsync(m => m.HermodUserId == message.RecipientUserId);

        if (mapping is null)
        {
            logger.LogDebug(
                "No Discord user mapping for Hermod user {UserId}, skipping file distribution",
                message.RecipientUserId);
            return;
        }

        var user = await discord.Rest.GetUserAsync(mapping.DiscordUserId);
        if (user is null)
        {
            logger.LogWarning(
                "Discord user {DiscordUserId} not found via REST, skipping file distribution",
                mapping.DiscordUserId);
            return;
        }

        try
        {
            var dmChannel = await user.CreateDMChannelAsync();
            var bytes = Encoding.UTF8.GetBytes(message.FileContent);
            using var stream = new MemoryStream(bytes);

            await dmChannel.SendFileAsync(
                stream,
                message.FileName,
                "A play file has been shared with you!");

            logger.LogInformation(
                "Distributed play file {FileName} to Discord user {DiscordUserId}",
                message.FileName, mapping.DiscordUserId);
        }
        catch (HttpException ex)
        {
            logger.LogWarning(ex,
                "Failed to DM play file to Discord user {DiscordUserId} — DMs may be disabled",
                mapping.DiscordUserId);
        }
    }
}
