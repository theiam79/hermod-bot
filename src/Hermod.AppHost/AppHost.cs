var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);
var discordClientId = builder.AddParameter("discord-client-id");
var discordClientSecret = builder.AddParameter("discord-client-secret", secret: true);

var skipBot = builder.Configuration["SkipBot"] is "true" or "True";
var useTunnel = builder.Configuration["UseTunnel"] is "true" or "True";

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();
var hermodDb = postgres.AddDatabase("hermod-db");
var authDb = postgres.AddDatabase("auth-db");
var botDb = postgres.AddDatabase("bot-db");

var nats = builder.AddNats("nats");

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api")
    .WithReference(hermodDb)
    .WithReference(authDb)
    .WithReference(nats)
    .WithEnvironment("Discord__ClientId", discordClientId)
    .WithEnvironment("Discord__ClientSecret", discordClientSecret)
    .WaitFor(hermodDb)
    .WaitFor(authDb)
    .WaitFor(nats);

var bot = builder.AddProject<Projects.Hermod_Bot>("hermod-bot")
    .WithReference(botDb)
    .WithReference(nats)
    .WithEnvironment("Discord__Token", discordToken)
    .WaitFor(botDb)
    .WaitFor(nats);

if (skipBot)
{
    bot.WithExplicitStart();
}

var frontend = builder.AddViteApp("hermod-web", "../Hermod.Web")
    .WithReference(api)
    .WithEndpoint("http", ep =>
    {
        ep.IsProxied = false;
        ep.Port = builder.ExecutionContext.IsRunMode ? 5173 : null;
    })
    ;

var gateway = builder.AddYarp("hermod-gateway")
    .WithHostPort(8080)
    .WithHostHttpsPort(8443)
    .WithConfiguration(yarp =>
    {
        // Publish mode: YARP routes API traffic directly
        if (builder.ExecutionContext.IsPublishMode)
        {
            yarp.AddRoute("api/{**catch-all}", api);
            yarp.AddRoute("auth/{**catch-all}", api);
            yarp.AddRoute("signin-discord", api);
        }

        // Run mode: everything goes to Vite (its proxy handles API routing)
        // Publish mode: catch-all serves static files via PublishWithStaticFiles
        if (builder.ExecutionContext.IsRunMode)
        {
            yarp.AddRoute("{**catch-all}", frontend);
        }
    })
    .WithExternalHttpEndpoints()
    .PublishWithStaticFiles(frontend);

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

builder.Build().Run();
