using System.Text;
using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Rest;

namespace Hermod.Bot.Handlers;

public static class DistributePlayFileHandler
{
    public static async Task Handle(
        DistributePlayFile message,
        BotDbContext db,
        RestClient rest,
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

        try
        {
            var dmChannel = await rest.GetDMChannelAsync(mapping.DiscordUserId);
            var bytes = Encoding.UTF8.GetBytes(message.FileContent);
            using var stream = new MemoryStream(bytes);

            await rest.SendMessageAsync(dmChannel.Id, new MessageProperties
            {
                Content = "A play file has been shared with you!",
                Attachments = [new AttachmentProperties(message.FileName, stream)],
            });

            logger.LogInformation(
                "Distributed play file {FileName} to Discord user {DiscordUserId}",
                message.FileName, mapping.DiscordUserId);
        }
        catch (RestException ex)
        {
            logger.LogWarning(ex,
                "Failed to DM play file to Discord user {DiscordUserId} — DMs may be disabled",
                mapping.DiscordUserId);
        }
    }
}
