using Hermod.Bot.Data;
using Hermod.Bot.Infrastructure;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using Wolverine;
using Wolverine.Nats;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var natsUrl = builder.Configuration.GetConnectionString("nats")
    ?? throw new InvalidOperationException("ConnectionStrings:nats is not configured.");
var isTesting = builder.Configuration["Testing:Enabled"] is "true";

builder.AddNpgsqlDbContext<BotDbContext>("bot-db");

if (!isTesting)
{
    // PRODUCTION: Full NetCord gateway + slash commands + interaction handlers
    builder.Services
        .AddDiscordGateway(options =>
        {
            options.Intents = GatewayIntents.Guilds;
        })
        .AddApplicationCommands<SlashCommandInteraction, SlashCommandContext>()
        .AddApplicationCommands<MessageCommandInteraction, MessageCommandContext>()
        .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>()
        .AddGatewayHandlers(typeof(Program).Assembly);
}
else
{
    // TESTING: Standalone RestClient pointed at WireMock (no gateway, no slash commands)
    var discordApiHost = builder.Configuration["Discord:ApiBaseUrl"]
        ?? throw new InvalidOperationException("Discord:ApiBaseUrl is required in Testing mode.");
    var token = new BotToken(builder.Configuration["Discord:Token"] ?? "test-token");
    builder.Services.AddSingleton(new RestClient(token, new RestClientConfiguration
    {
        Hostname = discordApiHost,
        RequestHandler = new HttpDowngradeHandler(),
    }));
}

builder.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

    opts.UseNats(natsUrl)
        .AutoProvision();

    opts.ListenToNatsSubject("hermod.bot");

    opts.PublishMessage<RegisterCommunity>()
        .ToNatsSubject("hermod.api");

    opts.PublishMessage<UpdateGroupSharing>()
        .ToNatsSubject("hermod.api");

    opts.PublishMessage<EnrollInGroup>()
        .ToNatsSubject("hermod.api");

    opts.PublishMessage<ClaimPlayer>()
        .ToNatsSubject("hermod.api");

    opts.PublishMessage<LeaveGroup>()
        .ToNatsSubject("hermod.api");
});

var host = builder.Build();

// Apply Bot migrations
using (var scope = host.Services.CreateScope())
{
    var botDb = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    await botDb.Database.MigrateAsync();
}

if (!isTesting)
{
    host.AddModules(typeof(Program).Assembly);
}

await host.RunAsync();

public partial class Program;
