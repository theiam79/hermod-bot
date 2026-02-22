var builder = DistributedApplication.CreateBuilder(args);

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

builder.Build().Run();
