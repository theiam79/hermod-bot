using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Hermod.Api.Discord;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

builder.Services.AddDbContextWithWolverineIntegration<HermodContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HermodDb") ?? "Data Source=hermod.db"));

builder.Host
    .ConfigureDiscordHost((context, config) =>
    {
        config.Token = context.Configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord:Token is not configured.");
        config.SocketConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200,
            GatewayIntents =
                GatewayIntents.Guilds |
                GatewayIntents.GuildMembers |    // privileged — enable in Discord dev portal
                GatewayIntents.GuildMessages |
                GatewayIntents.MessageContent    // privileged — required to read attachments
        };
    })
    .UseInteractionService((context, config) =>
    {
        config.LogLevel = LogSeverity.Info;
        config.UseCompiledLambda = true;
    });

builder.Services.AddHostedService<BotService>();
builder.Services.AddHostedService<InteractionHandler>();
builder.Services.AddHostedService<GuildHandler>();
builder.Services.AddHostedService<MessageReceivedHandler>();

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
