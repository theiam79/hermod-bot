using Hermod.Api.Messages;
using Hermod.BGStats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public static class UploadPlays
{
    [WolverinePost("/api/plays/upload")]
    public static async Task<(IResult, OutgoingMessages)> Post(
        IFormFile file,
        [FromQuery] Guid? groupId)
    {
        await using var stream = file.OpenReadStream();
        var result = await PlayFileParser.ParseAsync(stream);

        var messages = new OutgoingMessages();
        foreach (var play in result.Plays)
        {
            messages.Add(new PlayExtracted(play, groupId));
        }

        return (Results.Accepted(), messages);
    }
}
