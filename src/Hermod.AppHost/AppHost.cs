var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);
var discordClientId = builder.AddParameter("discord-client-id");
var discordClientSecret = builder.AddParameter("discord-client-secret", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var hermodDb = postgres.AddDatabase("hermod-db");
var authDb = postgres.AddDatabase("auth-db");

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api")
    .WithReference(hermodDb)
    .WithReference(authDb)
    .WithEnvironment("Discord__ClientId", discordClientId)
    .WithEnvironment("Discord__ClientSecret", discordClientSecret)
    .WaitFor(hermodDb)
    .WaitFor(authDb);

var bot = builder.AddProject<Projects.Hermod_Bot>("hermod-bot")
    .WithReference(hermodDb)
    .WithEnvironment("Discord__Token", discordToken)
    .WaitFor(hermodDb);

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
        yarp.AddRoute("api/{**catch-all}", api);
        yarp.AddRoute("auth/{**catch-all}", api);
        yarp.AddRoute("signin-discord", api);

        if (builder.ExecutionContext.IsRunMode)
        {
            yarp.AddRoute("{**catch-all}", frontend);
        }
    })
    .WithExternalHttpEndpoints()
    .PublishWithStaticFiles(frontend);

builder.Build().Run();
