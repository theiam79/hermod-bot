using Hermod.Api.Messages;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class PlayCreatedHandler
{
    public static PostPlay? Handle(PlayCreated message, ILogger logger)
    {
        logger.LogInformation("Play {PlayId} created for group {GroupId}",
            message.PlayId, message.GroupId);

        if (!message.GroupId.HasValue)
            return null;   // no group context — skip posting

        return new PostPlay(message.PlayId, message.GroupId.Value);
    }
}
