using System.Net;
using System.Net.Http.Json;
using TUnit.Core;

namespace Hermod.Api.Tests.Endpoints;

public class GroupEndpointTests
{
    [ClassDataSource<ApiFixture>(Shared = SharedType.PerTestSession)]
    public required ApiFixture Api { get; init; }

    [Test]
    public async Task CreateGroup_ReturnsCreated()
    {
        var client = Api.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/groups", new { Name = "Test Group" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<GroupResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Name).IsEqualTo("Test Group");
        await Assert.That(body.Id).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task CreateAndGetGroup_RoundTrips()
    {
        var client = Api.CreateAnonymousClient();

        var createResponse = await client.PostAsJsonAsync("/api/groups", new { Name = "Round Trip Group" });
        var created = await createResponse.Content.ReadFromJsonAsync<GroupResponse>();

        var getResponse = await client.GetAsync($"/api/groups/{created!.Id}");

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<GroupResponse>();
        await Assert.That(fetched).IsNotNull();
        await Assert.That(fetched!.Name).IsEqualTo("Round Trip Group");
        await Assert.That(fetched.Id).IsEqualTo(created.Id);
    }

    [Test]
    public async Task GetGroup_NotFound_Returns404()
    {
        var client = Api.CreateAnonymousClient();

        var response = await client.GetAsync($"/api/groups/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private record GroupResponse(Guid Id, string Name, bool AllowSharing);
}
