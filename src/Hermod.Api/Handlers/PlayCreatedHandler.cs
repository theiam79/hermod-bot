using Hermod.Api.Messages;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class PlayCreatedHandler
{
    public static PostPlay? Handle(PlayCreated message, ILogger logger)
    {
        logger.LogInformation("Play {PlayId} created", message.PlayId);

        // No group context available — skip posting (will be replaced by SharePlayHandler)
        return null;
    }
}
