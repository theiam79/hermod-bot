var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api")
    .WithEnvironment("Discord__Token", discordToken);

builder.Build().Run();
