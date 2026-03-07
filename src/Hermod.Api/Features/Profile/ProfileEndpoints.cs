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

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null)
        {
            var displayName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            profile = new UserProfileEntity
            {
                Id = userId,
                DisplayName = displayName,
            };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();
        }

        var groups = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => new GroupSummary(ug.GroupId.Value, ug.Group.Name))
            .ToListAsync();

        return Results.Ok(new ProfileResponse(
            profile.Id.Value,
            profile.DisplayName,
            profile.BggId,
            profile.BggUsername,
            profile.SubscribeToPlays,
            profile.PostingEnabled,
            profile.DistributionEnabled,
            groups));
    }

    [Authorize]
    [WolverinePut("/api/profile")]
    public static async Task<IResult> Put(UpdateProfileRequest request, ClaimsPrincipal user, [FromServices] HermodContext db)
    {
        var userId = UserId.From(user.GetUserId()!.Value);

        var profile = await db.UserProfiles.FindAsync(userId);
        if (profile is null)
        {
            var displayName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            profile = new UserProfileEntity
            {
                Id = userId,
                DisplayName = displayName,
            };
            db.UserProfiles.Add(profile);
        }

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

        var groups = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => new GroupSummary(ug.GroupId.Value, ug.Group.Name))
            .ToListAsync();

        return Results.Ok(new ProfileResponse(
            profile.Id.Value,
            profile.DisplayName,
            profile.BggId,
            profile.BggUsername,
            profile.SubscribeToPlays,
            profile.PostingEnabled,
            profile.DistributionEnabled,
            groups));
    }
}
