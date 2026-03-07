using System.Net;
using System.Net.Http.Json;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Hermod.Api.Tests.Endpoints;

public class ProfileEndpointTests
{
    [ClassDataSource<ApiFixture>(Shared = SharedType.PerTestSession)]
    public required ApiFixture Api { get; init; }

    private async Task<(Guid userId, Guid groupId)> SeedProfileWithGroup()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await using var scope = Api.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();

        var group = new GroupEntity { Id = GroupId.From(groupId), Name = "Profile Test Group" };
        db.Groups.Add(group);

        var profile = new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "TestProfile",
            BggUsername = "testbgg",
            BggId = 42,
            SubscribeToPlays = true
        };
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        db.UserGroups.Add(new UserGroupEntity
        {
            UserId = UserId.From(userId),
            GroupId = GroupId.From(groupId),
            Role = GroupRole.Member,
        });
        await db.SaveChangesAsync();

        return (userId, groupId);
    }

    // --- GET /api/profile ---

    [Test]
    public async Task GetProfile_ReturnsProfileWithGroupList()
    {
        var (userId, groupId) = await SeedProfileWithGroup();
        var client = Api.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/profile");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.UserId).IsEqualTo(userId);
        await Assert.That(body.DisplayName).IsEqualTo("TestProfile");
        await Assert.That(body.BggUsername).IsEqualTo("testbgg");
        await Assert.That(body.BggId).IsEqualTo(42);
        await Assert.That(body.SubscribeToPlays).IsTrue();
        await Assert.That(body.Groups.Count).IsEqualTo(1);
        await Assert.That(body.Groups[0].GroupId).IsEqualTo(groupId);
        await Assert.That(body.Groups[0].Name).IsEqualTo("Profile Test Group");
    }

    [Test]
    public async Task GetProfile_NoProfile_CreatesAndReturnsProfile()
    {
        var client = Api.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.GetAsync("/api/profile");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.DisplayName).IsEqualTo("TestUser");
        await Assert.That(body.Groups.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetProfile_Anonymous_ReturnsUnauthorized()
    {
        var client = Api.CreateAnonymousClient();

        var response = await client.GetAsync("/api/profile");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- PUT /api/profile ---

    [Test]
    public async Task PutProfile_UpdateDisplayName_ReturnsUpdated()
    {
        var (userId, _) = await SeedProfileWithGroup();
        var client = Api.CreateAuthenticatedClient(userId);

        var response = await client.PutAsJsonAsync("/api/profile",
            new { DisplayName = "NewName" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.DisplayName).IsEqualTo("NewName");
        // Other fields unchanged
        await Assert.That(body.BggUsername).IsEqualTo("testbgg");
        await Assert.That(body.BggId).IsEqualTo(42);
    }

    [Test]
    public async Task PutProfile_UpdateBggUsername_ReturnsUpdated()
    {
        var userId = Guid.NewGuid();
        await using var scope = Api.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "BggUser",
            BggUsername = "oldbgg"
        });
        await db.SaveChangesAsync();

        var client = Api.CreateAuthenticatedClient(userId);

        var response = await client.PutAsJsonAsync("/api/profile",
            new { BggUsername = "newbgg" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.BggUsername).IsEqualTo("newbgg");
        await Assert.That(body.DisplayName).IsEqualTo("BggUser");
    }

    [Test]
    public async Task PutProfile_UpdateSubscribeToPlays_ReturnsUpdated()
    {
        var userId = Guid.NewGuid();
        await using var scope = Api.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "SubUser",
            SubscribeToPlays = true
        });
        await db.SaveChangesAsync();

        var client = Api.CreateAuthenticatedClient(userId);

        var response = await client.PutAsJsonAsync("/api/profile",
            new { SubscribeToPlays = false });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.SubscribeToPlays).IsFalse();
    }

    [Test]
    public async Task PutProfile_DisplayNameTooLong_ReturnsValidationProblem()
    {
        var userId = Guid.NewGuid();
        await using var scope = Api.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();
        db.UserProfiles.Add(new UserProfileEntity
        {
            Id = UserId.From(userId),
            DisplayName = "ValidUser"
        });
        await db.SaveChangesAsync();

        var client = Api.CreateAuthenticatedClient(userId);

        var longName = new string('a', 201);
        var response = await client.PutAsJsonAsync("/api/profile",
            new { DisplayName = longName });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PutProfile_NoProfile_CreatesAndUpdatesProfile()
    {
        var client = Api.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.PutAsJsonAsync("/api/profile",
            new { DisplayName = "NewlyCreated" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.DisplayName).IsEqualTo("NewlyCreated");
    }

    [Test]
    public async Task PutProfile_Anonymous_ReturnsUnauthorized()
    {
        var client = Api.CreateAnonymousClient();

        var response = await client.PutAsJsonAsync("/api/profile",
            new { DisplayName = "Nope" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private record GroupSummary(Guid GroupId, string Name);

    private record ProfileResponse(
        Guid UserId,
        string DisplayName,
        int? BggId,
        string? BggUsername,
        bool SubscribeToPlays,
        List<GroupSummary> Groups);
}
