using NetCord.Rest;

namespace Hermod.Bot.Infrastructure;

/// <summary>
/// Rewrites HTTPS requests to HTTP for use with WireMock in Testing mode.
/// NetCord hardcodes "https://" in its base URL construction, but WireMock
/// listens on HTTP by default.
/// </summary>
public sealed class HttpDowngradeHandler : IRestRequestHandler
{
    private readonly HttpClient _http = new();

    public void AddDefaultHeader(string name, IEnumerable<string> values)
        => _http.DefaultRequestHeaders.Add(name, values);

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var uri = request.RequestUri!;
        request.RequestUri = new UriBuilder(uri) { Scheme = "http", Port = uri.Port }.Uri;
        return _http.SendAsync(request, cancellationToken);
    }

    public void Dispose() => _http.Dispose();
}
