using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Discord;

public class BotService(
    DiscordSocketClient client,
    ILogger<BotService> logger)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Log += LogAsync;
        Client.Ready += OnReadyAsync;

        await Client.StartAsync();
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task LogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error    => LogLevel.Error,
            LogSeverity.Warning  => LogLevel.Warning,
            LogSeverity.Info     => LogLevel.Information,
            LogSeverity.Verbose  => LogLevel.Debug,
            LogSeverity.Debug    => LogLevel.Trace,
            _                    => LogLevel.Information,
        };

        logger.Log(level, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        logger.LogInformation(
            "Connected as {Username}#{Discriminator} serving {GuildCount} guild(s)",
            Client.CurrentUser.Username,
            Client.CurrentUser.Discriminator,
            Client.Guilds.Count);
        return Task.CompletedTask;
    }
}
