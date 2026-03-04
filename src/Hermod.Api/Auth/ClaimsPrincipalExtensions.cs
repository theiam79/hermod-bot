using System.Security.Claims;

namespace Hermod.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public const string UserIdClaimType = "hermod:user_id";

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(UserIdClaimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
