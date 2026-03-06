using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Hermod.E2E.Tests.Infrastructure;

public static class WireMockHelper
{
    /// <summary>
    /// Configure default Discord REST API stubs. Called once during fixture setup.
    /// Covers the 4 Discord API calls the bot makes:
    ///   - POST /channels/{id}/messages (send embed)
    ///   - PATCH /channels/{id}/messages/{id} (edit embed)
    ///   - POST /users/@me/channels (open DM channel)
    /// </summary>
    public static void ConfigureDiscordStubs(WireMockServer server)
    {
        // POST /api/v10/channels/{id}/messages → send message
        server
            .Given(Request.Create()
                .UsingPost()
                .WithPath("/api/v10/channels/*/messages"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "id": "100000000000000001",
                        "channel_id": "mock-channel",
                        "content": "",
                        "timestamp": "2026-01-01T00:00:00Z",
                        "author": { "id": "200000000000000001", "username": "Hermod" }
                    }
                    """));

        // PATCH /api/v10/channels/{id}/messages/{id} → edit message
        server
            .Given(Request.Create()
                .UsingPatch()
                .WithPath("/api/v10/channels/*/messages/*"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "id": "100000000000000001",
                        "channel_id": "mock-channel",
                        "content": "",
                        "timestamp": "2026-01-01T00:00:00Z",
                        "author": { "id": "200000000000000001", "username": "Hermod" }
                    }
                    """));

        // POST /api/v10/users/@me/channels → create DM channel
        server
            .Given(Request.Create()
                .UsingPost()
                .WithPath("/api/v10/users/@me/channels"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "id": "999999999999999999",
                        "type": 1
                    }
                    """));

        // Catch-all for unexpected Discord API calls → 404
        server
            .Given(Request.Create()
                .WithPath("/api/v10/*"))
            .AtPriority(100)
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody("WireMock: unmatched Discord API route"));
    }

    /// <summary>
    /// Get all POST requests to /channels/*/messages (message sends).
    /// </summary>
    public static IReadOnlyList<WireMock.Logging.ILogEntry> GetSendMessageRequests(WireMockServer server)
        => server.FindLogEntries(
            Request.Create().UsingPost().WithPath("/api/v10/channels/*/messages"));

    /// <summary>
    /// Get all PATCH requests to /channels/*/messages/* (message edits).
    /// </summary>
    public static IReadOnlyList<WireMock.Logging.ILogEntry> GetEditMessageRequests(WireMockServer server)
        => server.FindLogEntries(
            Request.Create().UsingPatch().WithPath("/api/v10/channels/*/messages/*"));

    /// <summary>
    /// Reset the request log between tests (stubs remain configured).
    /// </summary>
    public static void ResetRequestLog(WireMockServer server)
        => server.ResetLogEntries();
}
