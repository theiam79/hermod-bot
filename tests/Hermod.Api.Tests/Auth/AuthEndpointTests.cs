using System.Net;
using System.Net.Http.Json;
using TUnit.Core;

namespace Hermod.Api.Tests.Auth;

public class AuthEndpointTests
{
    [ClassDataSource<ApiFixture>(Shared = SharedType.PerTestSession)]
    public required ApiFixture Api { get; init; }

    [Test]
    public async Task GetMe_WithoutAuth_Returns401()
    {
        var client = Api.CreateAnonymousClient();

        var response = await client.GetAsync("/auth/me");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMe_WithAuth_Returns200WithUserId()
    {
        var userId = Guid.NewGuid();
        var client = Api.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/auth/me");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.UserId).IsEqualTo(userId.ToString());
        await Assert.That(body.Username).IsEqualTo("TestUser");
    }

    private record MeResponse(string? UserId, string? Username, string? AvatarUrl);
}
