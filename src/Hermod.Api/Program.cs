using Hermod.Core.Extensions;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddDbContextFactory<HermodContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HermodDb") ?? "Data Source=hermod.db"));

builder.Services.AddHermodCore();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new { Name = "Hermod API", Status = "Running" }));

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HermodContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();
