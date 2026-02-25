var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);
var discordClientId = builder.AddParameter("discord-client-id");
var discordClientSecret = builder.AddParameter("discord-client-secret", secret: true);

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
    .WithEnvironment("WebApp__BaseUrl", "http://localhost:8080")
    .WaitFor(botDb)
    .WaitFor(nats);

var frontend = builder.AddViteApp("hermod-web", "../Hermod.Web")
    .WithReference(api)
    .WithEndpoint("http", ep =>
    {
        ep.IsProxied = false;
        ep.Port = builder.ExecutionContext.IsRunMode ? 5173 : null;
    })
    ;

builder.AddYarp("hermod-gateway")
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

builder.Build().Run();
