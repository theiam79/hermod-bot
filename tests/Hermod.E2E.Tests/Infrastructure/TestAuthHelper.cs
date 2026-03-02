using Hermod.Auth;

namespace Hermod.E2E.Tests.Infrastructure;

/// <summary>
/// Seeds auth-db with a test user and obtains a session cookie via the
/// Testing-only /auth/test-login endpoint.
/// </summary>
public static class TestAuthHelper
{
    /// <summary>
    /// Creates an AuthUser in auth-db and returns an HttpClient with a valid session cookie.
    /// The returned client is an Aspire CreateHttpClient with the session cookie
    /// pre-set as a default request header.
    /// </summary>
    public static async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync(
        AspireFixture fixture,
        string displayName = "E2ETestUser")
    {
        var userId = Guid.NewGuid();

        // Seed an AuthUser in auth-db so the session maps to a real user
        await using var authDb = fixture.CreateAuthDb();
        authDb.Users.Add(new AuthUser
        {
            Id = userId,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        await authDb.SaveChangesAsync();

        // Obtain a session cookie via the Testing-only endpoint.
        // Use a temporary client to POST and extract the Set-Cookie header.
        string cookieHeader;
        using (var loginClient = fixture.CreateApiClient())
        {
            var response = await loginClient.PostAsync($"/auth/test-login?userId={userId}", null);
            response.EnsureSuccessStatusCode();

            // Extract all Set-Cookie values and join as a Cookie header
            if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                throw new InvalidOperationException("test-login did not return a Set-Cookie header");

            cookieHeader = string.Join("; ",
                setCookies.Select(c => c.Split(';', 2)[0].Trim()));
        }

        // Create a fresh client with the session cookie pre-set
        var client = fixture.CreateApiClient();
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

        return (client, userId);
    }
}
