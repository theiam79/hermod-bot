using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SerilogTimings.Extensions;
using Microsoft.Extensions.Logging;

namespace Hermod.Bot
{

    internal class CommandHandler : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandService _commandService;
        private readonly BotOptions _botOptions;
        
        public CommandHandler(
            DiscordSocketClient client,
            ILogger<CommandHandler> logger,
            IServiceProvider provider,
            CommandService commandService,
            IOptions<BotOptions> botOptions
            ) : base(client, logger)
        {
            _serviceProvider = provider;
            _commandService = commandService;
            _botOptions = botOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.MessageReceived += HandleMessage;
            _commandService.CommandExecuted += CommandExecutedAsync;

            Logger.LogInformation("Searching for modules to load");
            await _commandService.AddModuleAsync<Modules.Info.Module>(_serviceProvider);
            await _commandService.AddModuleAsync<Modules.Share.Module>(_serviceProvider);
        }

        private async Task HandleMessage(SocketMessage incomingMessage)
        {
            if (incomingMessage is not SocketUserMessage message) return;
            if (message.Source != MessageSource.User) return;

            Logger.BeginScope(LogContext());

            var context = new SocketCommandContext(Client, message);

            var playFile = message.Attachments.FirstOrDefault(a => a.Filename.EndsWith(".bgsplay"));

            if (playFile != default)
            {
                await _commandService.ExecuteAsync(context, "share", _serviceProvider.CreateScope().ServiceProvider);
                return;
            }

            int argPos = 0;
            if (!message.HasStringPrefix(_botOptions.Prefix, ref argPos) && !message.HasMentionPrefix(Client.CurrentUser, ref argPos)) return;

            await _commandService.ExecuteAsync(context, argPos, _serviceProvider.CreateScope().ServiceProvider);
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            Logger.LogInformation("{User} attempted to use command {command}", command.IsSpecified ? command.Value.Name : "Unknown");

            if (!command.IsSpecified || result.IsSuccess)
                return;

            await context.Channel.SendMessageAsync($"Error: {result}");
        }

        private Dictionary<string, object> LogContext() => new()
        {
            ["CorrelationId"] = Correlate(),
            ["User"] = Client.CurrentUser.Username
        };

        private static Guid Correlate() => Guid.NewGuid();
    }
}
