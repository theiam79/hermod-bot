using Hermod.Data.Context;
using Hermod.Testing;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddDbContext<HermodContext>(o => o.UseSqlite("FileName=./data/Hermod.db"));
        services.AddHostedService<Worker>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{


    System.IO.Directory.CreateDirectory("./data");
    var context = scope.ServiceProvider.GetRequiredService<HermodContext>();
    await context.Database.EnsureCreatedAsync();
}

await host.RunAsync();
