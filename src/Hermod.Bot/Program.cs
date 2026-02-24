using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Hermod.Bot.Data;
using Hermod.Bot.Services;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Postgresql;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var hermodConnectionString = builder.Configuration.GetConnectionString("hermod-db")
    ?? throw new InvalidOperationException("Missing required connection string 'hermod-db'.");

builder.AddNpgsqlDbContext<BotDbContext>("hermod-db");

builder.Services.AddDiscordHost((config, _) =>
{
    config.SocketConfig = new DiscordSocketConfig
    {
        LogLevel = LogSeverity.Info,
        GatewayIntents = GatewayIntents.Guilds,
    };

    config.Token = builder.Configuration["Discord:Token"]
        ?? throw new InvalidOperationException("Discord:Token is not configured.");
});

builder.Services.AddHostedService<GuildEventService>();

builder.UseWolverine(opts =>
{
    opts.PersistMessagesWithPostgresql(hermodConnectionString);
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
    opts.ListenToPostgresqlQueue("bot-inbox");

    opts.PublishMessage<RegisterGuild>()
        .ToPostgresqlQueue("api-inbox");

    opts.PublishMessage<UpdateGroupSharing>()
        .ToPostgresqlQueue("api-inbox");
});

var host = builder.Build();

// Apply Bot migrations
using (var scope = host.Services.CreateScope())
{
    var botDb = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    await botDb.Database.MigrateAsync();
}

host.Run();

public partial class Program;
