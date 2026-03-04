using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Messages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Wolverine.Http;

namespace Hermod.Api.Features.Auth;

public static class AuthEndpoints
{
    [WolverineGet("/auth/login")]
    public static IResult Login(HttpContext context, string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        return Results.Challenge(properties, [Providers.Discord]);
    }

    [WolverineGet("/auth/me")]
    public static IResult Me(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userId = user.FindFirstValue(ClaimsPrincipalExtensions.UserIdClaimType);
        var username = user.FindFirstValue(ClaimTypes.Name);
        var avatarUrl = user.FindFirstValue("urn:discord:avatar:url");

        return Results.Ok(new
        {
            UserId = userId,
            Username = username,
            AvatarUrl = avatarUrl,
        });
    }

    [WolverinePost("/auth/logout")]
    public static async Task<IResult> Logout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }
}
