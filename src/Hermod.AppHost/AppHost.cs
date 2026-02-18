var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api");

builder.Build().Run();
