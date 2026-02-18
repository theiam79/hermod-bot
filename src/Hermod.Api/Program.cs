using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddDbContextWithWolverineIntegration<HermodContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HermodDb") ?? "Data Source=hermod.db"));

builder.Host.UseWolverine(opts =>
{
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWolverineEndpoints();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HermodContext>();
    await context.Database.MigrateAsync();
}

app.Run();
