using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Hermod.Api.Auth;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var header = authHeader.ToString();
        if (!header.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var providedKey = header["ApiKey ".Length..].Trim();
        var expectedKey = configuration["ApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
            return Task.FromResult(AuthenticateResult.Fail("API key is not configured on the server."));

        if (!string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "hermod-bot"),
            new Claim(ClaimTypes.Role, "Service"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
