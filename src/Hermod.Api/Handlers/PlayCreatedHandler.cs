using Hermod.Api.Messages;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class PlayCreatedHandler
{
    public static void Handle(PlayCreated message, ILogger logger)
    {
        logger.LogInformation(
            "Play {PlayId} created in group {GroupId}",
            message.PlayId,
            message.GroupId);
    }
}
