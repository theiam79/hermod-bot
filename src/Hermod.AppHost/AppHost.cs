var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var hermodDb = postgres.AddDatabase("hermod-db");

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api")
    .WithEnvironment("Discord__Token", discordToken)
    .WithReference(hermodDb)
    .WaitFor(hermodDb);

builder.Build().Run();
