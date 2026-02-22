using Hermod.Api.Messages;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class PlayCreatedHandler
{
    public static PostPlay? Handle(PlayPersisted message, ILogger logger)
    {
        logger.LogInformation("Play {PlayId} persisted ({ChangeType})",
            message.PlayId, message.ChangeType);

        // No group context available — skip posting (will be replaced by SharePlayHandler)
        return null;
    }
}
