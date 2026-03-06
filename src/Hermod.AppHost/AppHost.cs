var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOMPUTE003 // Container registry APIs are experimental
#pragma warning disable ASPIREPIPELINES003 // Pipeline APIs are experimental
var registryEndpoint = builder.AddParameter("registry-endpoint", "ghcr.io", publishValueAsDefault: true);
var registryRepository = builder.AddParameter("registry-repository", "theiam79", publishValueAsDefault: true);
var registryTag = builder.Configuration["Parameters:registry-tag"] ?? "latest";
var registry = builder.AddContainerRegistry("ghcr", registryEndpoint, registryRepository);

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api")
    .WithContainerRegistry(registry)
    .WithRemoteImageTag(registryTag);

var bot = builder.AddProject<Projects.Hermod_Bot>("hermod-bot")
    .WithContainerRegistry(registry)
    .WithRemoteImageTag(registryTag);

// Run mode: Aspire provisions containers and manages secrets from user secrets.
// Publish mode: K8s injects connection strings and secrets as env vars at deploy time.
if (builder.ExecutionContext.IsRunMode)
{
    var skipBot = builder.Configuration["SkipBot"] is "true" or "True";
    var useTunnel = builder.Configuration["UseTunnel"] is "true" or "True";

    var postgres = builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithPgAdmin();
    var hermodDb = postgres.AddDatabase("hermod-db");
    var authDb = postgres.AddDatabase("auth-db");
    var botDb = postgres.AddDatabase("bot-db");

    var nats = builder.AddNats("nats");

    var discordToken = builder.AddParameter("discord-token", secret: true);
    var discordClientId = builder.AddParameter("discord-client-id");
    var discordClientSecret = builder.AddParameter("discord-client-secret", secret: true);

    api
        .WithReference(hermodDb)
        .WithReference(authDb)
        .WithReference(nats)
        .WithEnvironment("Discord__ClientId", discordClientId)
        .WithEnvironment("Discord__ClientSecret", discordClientSecret)
        .WaitFor(hermodDb)
        .WaitFor(authDb)
        .WaitFor(nats);

    bot
        .WithReference(botDb)
        .WithReference(nats)
        .WithEnvironment("Discord__Token", discordToken)
        .WaitFor(botDb)
        .WaitFor(nats);

    var discordApiBaseUrl = builder.Configuration["Testing:DiscordApiBaseUrl"];
    if (discordApiBaseUrl is { Length: > 0 })
    {
        bot.WithEnvironment("Discord__ApiBaseUrl", discordApiBaseUrl);
        api.WithEnvironment("Testing__Enabled", "true");
        bot.WithEnvironment("Testing__Enabled", "true");
    }

    if (skipBot)
        bot.WithExplicitStart();

    var frontend = builder.AddViteApp("hermod-web", "../Hermod.Web")
        .WithReference(api)
        .WithEndpoint("http", ep =>
        {
            ep.IsProxied = false;
            ep.Port = 5173;
        });

    var gateway = builder.AddYarp("hermod-gateway")
        .WithHostPort(8080)
        .WithHostHttpsPort(8443)
        .WithConfiguration(yarp => yarp.AddRoute("{**catch-all}", frontend))
        .WithExternalHttpEndpoints()
        .WithContainerRegistry(registry)
        .WithRemoteImageTag(registryTag);

    if (useTunnel)
    {
        var tunnel = builder.AddDevTunnel("hermod-tunnel")
            .WithReference(gateway, allowAnonymous: true);

        var tunnelEndpoint = tunnel.GetEndpoint(gateway, "http");
        bot.WithEnvironment("WebApp__BaseUrl", tunnelEndpoint);
        api.WithEnvironment("Auth__ExternalBaseUrl", tunnelEndpoint);
    }
    else
    {
        bot.WithEnvironment("WebApp__BaseUrl", "http://localhost:8080");
    }
}
else
{
    // Publish mode: API serves static frontend files; gateway forwards everything
    var frontend = builder.AddViteApp("hermod-web", "../Hermod.Web")
        .WithReference(api);

    api.PublishWithContainerFiles(frontend, "wwwroot");

    var gateway = builder.AddYarp("hermod-gateway")
        .WithConfiguration(yarp => yarp.AddRoute("{**catch-all}", api))
        .WithExternalHttpEndpoints()
        .WithContainerRegistry(registry)
        .WithRemoteImageTag(registryTag);
}
#pragma warning restore ASPIREPIPELINES003
#pragma warning restore ASPIRECOMPUTE003

builder.Build().Run();
