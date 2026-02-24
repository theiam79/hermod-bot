using System.Security.Claims;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public record PlaySummary(
    Guid Id,
    string GameName,
    DateTime DatePlayed,
    string? Duration,
    string? LocationName,
    int PlayerCount,
    bool HasWinner);

public record PlaysResponse(
    List<PlaySummary> Plays,
    int TotalCount,
    int Page,
    int PageSize);

public static class PlayEndpoints
{
    public static IResult? Before(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue("hermod:user_id");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _))
            return Results.Unauthorized();

        return WolverineContinue.Result();
    }

    [WolverineGet("/api/plays")]
    public static async Task<PlaysResponse> Get(
        ClaimsPrincipal user,
        HermodContext db,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var userId = UserId.From(Guid.Parse(user.FindFirstValue("hermod:user_id")!));

        var query = db.Plays
            .Where(p => p.UploadedById == userId)
            .OrderByDescending(p => p.DatePlayed);

        var totalCount = await query.CountAsync();

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.GameName,
                p.DatePlayed,
                p.Duration,
                p.LocationName,
                PlayerCount = p.Players.Count,
                HasWinner = p.Players.Any(pp => pp.Winner),
            })
            .ToListAsync();

        var plays = rows.ConvertAll(p => new PlaySummary(
            p.Id.Value,
            p.GameName,
            p.DatePlayed,
            p.Duration is { } d ? FormatDuration(d) : null,
            p.LocationName,
            p.PlayerCount,
            p.HasWinner));

        return new PlaysResponse(plays, totalCount, page, pageSize);
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
            : $"{duration.Minutes}m";
}
