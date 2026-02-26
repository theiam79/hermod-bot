using System.Net;
using System.Net.Http.Json;
using System.Text;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Hermod.Api.Tests.Endpoints;

public class UploadEndpointTests
{
    [ClassDataSource<ApiFixture>(Shared = SharedType.PerTestSession)]
    public required ApiFixture Api { get; init; }

    private static readonly string ValidBgsplay = /*lang=json,strict*/ """
        {
          "players": [
            { "id": 1, "uuid": "aaaaaaaa-0000-0000-0000-000000000001", "name": "Alice", "isAnonymous": false, "modificationDate": "2024-01-01 00:00:00" }
          ],
          "locations": [
            { "id": 1, "uuid": "bbbbbbbb-0000-0000-0000-000000000001", "name": "Home", "modificationDate": "2024-01-01 00:00:00" }
          ],
          "games": [
            { "id": 1, "uuid": "cccccccc-0000-0000-0000-000000000001", "name": "Catan", "modificationDate": "2024-01-01 00:00:00", "highestWins": true, "isBaseGame": true }
          ],
          "plays": [
            {
              "uuid": "dddddddd-0000-0000-0000-000000000001",
              "modificationDate": "2024-01-01 00:00:00",
              "entryDate": "2024-01-01 00:00:00",
              "playDate": "2024-01-01 00:00:00",
              "durationMin": 60,
              "locationRefId": 1,
              "gameRefId": 1,
              "playerScores": [
                { "playerRefId": 1, "score": "10", "winner": true, "rank": 1 }
              ],
              "expansionPlays": []
            }
          ],
          "userInfo": { "meRefId": 1 }
        }
        """;

    private static MultipartFormDataContent CreateFileContent(string content, string fileName = "test.bgsplay")
    {
        var formContent = new MultipartFormDataContent();
        formContent.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", fileName);
        return formContent;
    }

    [Test]
    public async Task Upload_WithoutAuth_Returns401()
    {
        var client = Api.CreateAnonymousClient();

        using var content = CreateFileContent(ValidBgsplay);
        var response = await client.PostAsync("/api/plays/upload", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Upload_WithAuth_ReturnsAccepted()
    {
        var client = Api.CreateAuthenticatedClient(Guid.NewGuid());

        using var content = CreateFileContent(ValidBgsplay);
        var response = await client.PostAsync("/api/plays/upload", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<UploadResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.UploadId).IsNotEqualTo(Guid.Empty);
        await Assert.That(body.PlayCount).IsEqualTo(1);
    }

    [Test]
    public async Task Upload_OversizedFile_ReturnsBadRequest()
    {
        var client = Api.CreateAuthenticatedClient(Guid.NewGuid());

        var oversized = new string('x', 1_048_577); // 1 MB + 1 byte
        using var content = CreateFileContent(oversized);
        var response = await client.PostAsync("/api/plays/upload", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Upload_InvalidContent_ReturnsBadRequest()
    {
        var client = Api.CreateAuthenticatedClient(Guid.NewGuid());

        using var content = CreateFileContent("this is not json");
        var response = await client.PostAsync("/api/plays/upload", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Upload_PersistsUploadEntity()
    {
        var userId = Guid.NewGuid();
        var client = Api.CreateAuthenticatedClient(userId);

        using var content = CreateFileContent(ValidBgsplay);
        var response = await client.PostAsync("/api/plays/upload", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<UploadResponse>();
        await Assert.That(body).IsNotNull();

        await using var scope = Api.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<HermodContext>();
        var upload = await db.Uploads.FirstOrDefaultAsync(u => u.Id == UploadId.From(body!.UploadId));

        await Assert.That(upload).IsNotNull();
        await Assert.That(upload!.UploadedById).IsEqualTo(UserId.From(userId));
        await Assert.That(upload.FileName).IsEqualTo("test.bgsplay");
        await Assert.That(upload.FileContent).IsNotNull();
        await Assert.That(upload.FileContent.Length).IsGreaterThan(0);
    }

    private record UploadResponse(Guid UploadId, int PlayCount);
}
