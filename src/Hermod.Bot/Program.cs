using Wolverine;
using Wolverine.Postgresql;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var hermodConnectionString = builder.Configuration.GetConnectionString("hermod-db")
    ?? throw new InvalidOperationException("Missing required connection string 'hermod-db'.");

builder.UseWolverine(opts =>
{
    opts.PersistMessagesWithPostgresql(hermodConnectionString);
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
    opts.ListenToPostgresqlQueue("bot-inbox");
});

var host = builder.Build();
host.Run();

public partial class Program;
