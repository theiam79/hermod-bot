using Serilog;
using Serilog.Configuration;
using Discord.Addons.Hosting;
using Serilog.Enrichers.Sensitive;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Exceptions;
using Discord.WebSocket;
using Discord;
using Hermod.Bot;
using Hermod.Core.Extensions;
using Hermod.Data.Context;
using Microsoft.EntityFrameworkCore;
using Hermod.Bot.Options;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<CommandHandler>();
        services.AddHostedService<InteractionHandler>();
        services.AddHostedService<GuildHandler>();
        services
            .AddOptions<BotOptions>();
        //services.AddHostedService<Worker>();
        services.AddHermod();
        services.AddDbContext<HermodContext>(o => o.UseInMemoryDatabase("temp-testing"));
        //services.AddDbContext<HermodContext>(o => o.UseSqlite(""));
    })
    .UseSerilog((context, services, config) =>
    {
        config
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            //.Enrich.WithSensitiveDataMasking()
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers()
                .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() }));
    })
    .ConfigureDiscordHost((context, config) =>
    {
        config.SocketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200,
            UseInteractionSnowflakeDate = false
        };

        var token = context.Configuration["DiscordToken"];
        config.Token = token;
    })
    .UseCommandService((context, config) =>
    {
        config.LogLevel = LogSeverity.Verbose;
        config.DefaultRunMode = Discord.Commands.RunMode.Async;
        config.CaseSensitiveCommands = false;
    })
    .UseInteractionService((context, config) =>
    {
        config.LogLevel = LogSeverity.Verbose;
        config.DefaultRunMode = Discord.Interactions.RunMode.Async;
    })
    .Build();

await host.RunAsync();
