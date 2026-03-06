using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Hermod.Api.Features.Profile;

public record GroupSummary(Guid GroupId, string Name);

public record ProfileResponse(
    Guid UserId,
    string DisplayName,
    int? BggId,
    string? BggUsername,
    bool SubscribeToPlays,
    bool PostingEnabled,
    bool DistributionEnabled,
    List<GroupSummary> Groups);

public record UpdateProfileRequest(
    string? DisplayName,
    int? BggId,
    string? BggUsername,
    bool? SubscribeToPlays,
    bool? PostingEnabled,
    bool? DistributionEnabled);

public static class ProfileEndpoints
{
    [Authorize]
    [WolverineGet("/api/profile")]
    public static async Task<IResult> Get(ClaimsPrincipal user, HermodContext db)
    {
        var userId = UserId.From(user.GetUserId()!.Value);

        var profile = await db.UserProfiles
            .Include(p => p.UserGroups)
                .ThenInclude(ug => ug.Group)
            .FirstOrDefaultAsync(p => p.Id == userId);

        if (profile is null)
            return Results.NotFound();

        return Results.Ok(new ProfileResponse(
            profile.Id.Value,
            profile.DisplayName,
            profile.BggId,
            profile.BggUsername,
            profile.SubscribeToPlays,
            profile.PostingEnabled,
            profile.DistributionEnabled,
            profile.UserGroups.Select(ug => new GroupSummary(ug.GroupId.Value, ug.Group.Name)).ToList()));
    }

    [Authorize]
    [WolverinePut("/api/profile")]
    public static async Task<IResult> Put(UpdateProfileRequest request, ClaimsPrincipal user, [FromServices] HermodContext db)
    {
        var userId = UserId.From(user.GetUserId()!.Value);

        var profile = await db.UserProfiles.FindAsync(userId);
        if (profile is null)
            return Results.NotFound();

        if (request.DisplayName is not null)
        {
            if (request.DisplayName.Length > 200)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["DisplayName"] = ["Display name must be 200 characters or fewer."]
                });

            profile.DisplayName = request.DisplayName;
        }

        if (request.BggUsername is not null)
        {
            if (request.BggUsername.Length > 200)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["BggUsername"] = ["BGG username must be 200 characters or fewer."]
                });

            profile.BggUsername = request.BggUsername;
        }

        if (request.BggId is not null)
            profile.BggId = request.BggId;

        if (request.SubscribeToPlays is not null)
            profile.SubscribeToPlays = request.SubscribeToPlays.Value;

        if (request.PostingEnabled is not null)
            profile.PostingEnabled = request.PostingEnabled.Value;

        if (request.DistributionEnabled is not null)
            profile.DistributionEnabled = request.DistributionEnabled.Value;

        // Re-fetch with includes so response contains groups
        await db.SaveChangesAsync();
        var updated = await db.UserProfiles
            .Include(p => p.UserGroups)
                .ThenInclude(ug => ug.Group)
            .FirstAsync(p => p.Id == userId);

        return Results.Ok(new ProfileResponse(
            updated.Id.Value,
            updated.DisplayName,
            updated.BggId,
            updated.BggUsername,
            updated.SubscribeToPlays,
            updated.PostingEnabled,
            updated.DistributionEnabled,
            updated.UserGroups.Select(ug => new GroupSummary(ug.GroupId.Value, ug.Group.Name)).ToList()));
    }
}
