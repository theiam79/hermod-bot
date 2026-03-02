using System.Net;
using Hermod.E2E.Tests.Infrastructure;
using TUnit.Core;

namespace Hermod.E2E.Tests.Smoke;

public class AppHostSmokeTests
{
    [ClassDataSource<AspireFixture>(Shared = SharedType.PerTestSession)]
    public required AspireFixture Fixture { get; init; }

    [Test]
    public async Task ApiHealthEndpoint_ReturnsOk()
    {
        using var client = Fixture.CreateApiClient();
        var response = await client.GetAsync("/alive");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ApiIsReachable_ReturnsResponse()
    {
        using var client = Fixture.CreateApiClient();

        // /auth/me without auth should return 401 — proves the API is running
        var response = await client.GetAsync("/auth/me");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TestLogin_IssuesSessionCookie()
    {
        var (client, userId) = await TestAuthHelper.CreateAuthenticatedClientAsync(Fixture);
        using (client)
        {
            var response = await client.GetAsync("/auth/me");
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }
    }
}
