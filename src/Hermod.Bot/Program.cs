using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Hermod.Bot.Data;
using Hermod.Bot.Services;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Nats;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var natsUrl = builder.Configuration.GetConnectionString("nats")
    ?? throw new InvalidOperationException("ConnectionStrings:nats is not configured.");

builder.AddNpgsqlDbContext<BotDbContext>("bot-db");

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

builder.Services.AddInteractionService((config, _) =>
{
    config.DefaultRunMode = RunMode.Async;
    config.LogLevel = LogSeverity.Info;
});

builder.Services.AddHostedService<InteractionHandler>();
builder.Services.AddHostedService<GuildEventService>();

builder.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

    opts.UseNats(natsUrl)
        .AutoProvision();

    opts.ListenToNatsSubject("hermod.bot");

    opts.PublishMessage<RegisterGuild>()
        .ToNatsSubject("hermod.api");

    opts.PublishMessage<UpdateGroupSharing>()
        .ToNatsSubject("hermod.api");

    opts.PublishMessage<EnrollInGroup>()
        .ToNatsSubject("hermod.api");

    opts.PublishMessage<ClaimPlayer>()
        .ToNatsSubject("hermod.api");
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
