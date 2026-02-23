using Hermod.Messages;
using Microsoft.Extensions.Logging;

namespace Hermod.Bot.Handlers;

public static class SharePlayToGroupHandler
{
    public static void Handle(SharePlayToGroup message, ILogger logger)
    {
        logger.LogInformation(
            "Received {ChangeType} play {PlayId} for group {GroupId}: {GameName} ({PlayerCount} players)",
            message.ChangeType,
            message.PlayId,
            message.GroupId,
            message.Snapshot.GameName,
            message.Snapshot.Players.Count);
    }
}
