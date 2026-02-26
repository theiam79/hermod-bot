using System.Net;
using System.Net.Http.Json;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.Extensions.DependencyInjection;
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

    private async Task<(Guid userId, Guid groupId)> SeedUserAndGroup()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await using var scope = Api.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();
        db.Groups.Add(new GroupEntity { Id = GroupId.From(groupId), Name = "Enrollment Group" });
        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(userId), DisplayName = "EnrollUser" });
        await db.SaveChangesAsync();

        return (userId, groupId);
    }

    [Test]
    public async Task JoinGroup_Authenticated_ReturnsCreated()
    {
        var (userId, groupId) = await SeedUserAndGroup();
        var client = Api.CreateAuthenticatedClient(userId);

        var response = await client.PutAsync($"/api/groups/{groupId}/membership", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task JoinGroup_Twice_ReturnsNoContent()
    {
        var (userId, groupId) = await SeedUserAndGroup();
        var client = Api.CreateAuthenticatedClient(userId);

        await client.PutAsync($"/api/groups/{groupId}/membership", null);
        var response = await client.PutAsync($"/api/groups/{groupId}/membership", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task JoinGroup_Anonymous_ReturnsUnauthorized()
    {
        var client = Api.CreateAnonymousClient();

        var response = await client.PutAsync($"/api/groups/{Guid.NewGuid()}/membership", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task JoinGroup_NonexistentGroup_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        await using var scope = Api.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();
        db.UserProfiles.Add(new UserProfileEntity { Id = UserId.From(userId), DisplayName = "NoGroupUser" });
        await db.SaveChangesAsync();

        var client = Api.CreateAuthenticatedClient(userId);
        var response = await client.PutAsync($"/api/groups/{Guid.NewGuid()}/membership", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task JoinGroup_UserAppearsInGroupList()
    {
        var (userId, groupId) = await SeedUserAndGroup();
        var client = Api.CreateAuthenticatedClient(userId);

        await client.PutAsync($"/api/groups/{groupId}/membership", null);

        var listResponse = await client.GetAsync("/api/groups");
        var groups = await listResponse.Content.ReadFromJsonAsync<GroupResponse[]>();

        await Assert.That(groups).IsNotNull();
        await Assert.That(groups!.Any(g => g.Id == groupId)).IsTrue();
    }

    private record GroupResponse(Guid Id, string Name, bool AllowSharing);
}
