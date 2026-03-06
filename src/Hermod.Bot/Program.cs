using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using Wolverine;
using Wolverine.Nats;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var natsUrl = builder.Configuration.GetConnectionString("nats")
    ?? throw new InvalidOperationException("ConnectionStrings:nats is not configured.");

builder.AddNpgsqlDbContext<BotDbContext>("bot-db");

// NetCord: reads token from Discord:Token in IConfiguration automatically
builder.Services
    .AddDiscordGateway(options =>
    {
        options.Intents = GatewayIntents.Guilds;
    })
    .AddApplicationCommands<SlashCommandInteraction, SlashCommandContext>()
    .AddApplicationCommands<MessageCommandInteraction, MessageCommandContext>()
    .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>()
    .AddGatewayHandlers(typeof(Program).Assembly);

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

host.AddModules(typeof(Program).Assembly);
await host.RunAsync();

public partial class Program;
