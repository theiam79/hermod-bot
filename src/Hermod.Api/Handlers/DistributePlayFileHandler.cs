using Discord.WebSocket;
using Hermod.Api.Messages;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class DistributePlayFileHandler
{
    public static async Task Handle(
        DistributePlayFile message,
        HermodContext db,
        DiscordSocketClient discord,
        ILogger logger)
    {
        var mappings = await db.PlayerMappings
            .Where(pm => pm.BgStatsPlayerUuid == message.PlayerUuid)
            .ToListAsync();

        if (mappings.Count == 0) return;

        var upload = await db.Uploads
            .FirstOrDefaultAsync(u => u.Id == UploadId.From(message.UploadId));

        if (upload is null) return;

        foreach (var mapping in mappings)
        {
            try
            {
                var user = await db.Users
                    .FirstOrDefaultAsync(u => u.Id == mapping.MappedUserId);

                if (user is null || !user.SubscribeToPlays) continue;

                var login = await db.UserExternalLogins
                    .FirstOrDefaultAsync(l =>
                        l.UserId == mapping.MappedUserId && l.Provider == "Discord");

                if (login is null) continue;

                if (!ulong.TryParse(login.ProviderKey, out var discordUserId)) continue;

                var discordUser = await discord.GetUserAsync(discordUserId);
                if (discordUser is null) continue;

                var dmChannel = await discordUser.CreateDMChannelAsync();
                using var stream = new MemoryStream(upload.FileBytes);
                await dmChannel.SendFileAsync(
                    stream,
                    upload.FileName,
                    "Here's a play file you were in \u2014 import it into BGStats!");

                logger.LogInformation(
                    "Sent play file {FileName} to Discord user {UserId} for player UUID {Uuid}",
                    upload.FileName, discordUserId, message.PlayerUuid);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to DM play file to user mapped from UUID {Uuid}",
                    message.PlayerUuid);
            }
        }
    }
}
