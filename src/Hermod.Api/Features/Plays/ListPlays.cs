using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Features.Plays;

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

[Authorize]
public static class ListPlays
{
    [WolverineGet("/api/plays")]
    public static async Task<PlaysResponse> Get(
        ClaimsPrincipal user,
        HermodContext db,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var userId = UserId.From(user.GetUserId()!.Value);

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
