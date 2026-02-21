using System.Text;
using Hermod.BGStats;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public static class UploadPlays
{
    [WolverinePost("/api/plays/upload")]
    public static async Task<IResult> Post(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        var result = PlayFileParser.Parse(Encoding.UTF8.GetString(fileBytes));

        return Results.Ok(new
        {
            FileName = file.FileName,
            PlayCount = result.Plays.Count,
            Plays = result.Plays.Select(p => new
            {
                Game = p.Game.Name,
                p.DatePlayed,
                PlayerCount = p.Scores.Count,
            }),
        });
    }
}
