using System.Net;
using System.Net.Http.Json;
using TUnit.Core;

namespace Hermod.Api.Tests.Endpoints;

public class UploadEndpointTests
{
    [ClassDataSource<ApiFixture>(Shared = SharedType.PerTestSession)]
    public required ApiFixture Api { get; init; }

    [Test]
    public async Task UploadPlayFile_ReturnsParsedPlays()
    {
        var testFiles = Directory.GetFiles("TestData", "*.bgsplay", SearchOption.AllDirectories);
        if (testFiles.Length == 0)
            return;

        var client = Api.CreateAnonymousClient();

        var fileBytes = await File.ReadAllBytesAsync(testFiles[0]);
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileBytes), "file", Path.GetFileName(testFiles[0]));

        var response = await client.PostAsync("/api/plays/upload", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UploadResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.PlayCount).IsGreaterThan(0);
    }

    private record UploadResponse(string FileName, int PlayCount);
}
