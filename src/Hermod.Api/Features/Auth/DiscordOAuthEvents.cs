using System.Security.Claims;
using Hermod.Auth;
using Hermod.Messages;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Hermod.Api.Features.Auth;

public static class DiscordOAuthEvents
{
    public static async Task OnCreatingTicket(OAuthCreatingTicketContext context)
    {
        var discordId = context.Identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (discordId is null) return;

        var displayName = context.Identity?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var avatarUrl = context.User.GetProperty("avatar").GetString() is { } avatar
            ? $"https://cdn.discordapp.com/avatars/{discordId}/{avatar}.png"
            : null;

        var loginService = context.HttpContext.RequestServices.GetRequiredService<ExternalLoginService>();
        var (userId, _) = await loginService.ProvisionOrUpdateAsync(Providers.Discord, discordId, displayName, avatarUrl);

        context.Identity!.AddClaim(new Claim(Hermod.Api.Auth.ClaimsPrincipalExtensions.UserIdClaimType, userId.ToString()));
        if (avatarUrl is not null)
            context.Identity.AddClaim(new Claim("urn:discord:avatar:url", avatarUrl));
    }
}
