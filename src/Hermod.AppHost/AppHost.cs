var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("api-key", secret: true);

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api")
    .WithEnvironment("ApiKey", apiKey);

builder.Build().Run();
