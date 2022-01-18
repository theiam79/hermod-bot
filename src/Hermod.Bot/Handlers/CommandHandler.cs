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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CommandService _commandService;
        private readonly BotOptions _botOptions;
        
        public CommandHandler(
            DiscordSocketClient client,
            ILogger<CommandHandler> logger,
            IServiceScopeFactory scopeFactory,
            CommandService commandService,
            IOptions<BotOptions> botOptions
            ) : base(client, logger)
        {
            _scopeFactory = scopeFactory;
            _commandService = commandService;
            _botOptions = botOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.MessageReceived += HandleMessage;
            _commandService.CommandExecuted += CommandExecutedAsync;

            Logger.LogInformation("Searching for modules to load");
            await _commandService.AddModuleAsync<Modules.Info.Info>(_scopeFactory.CreateScope().ServiceProvider);
            await _commandService.AddModuleAsync<Modules.Share.Share>(_scopeFactory.CreateScope().ServiceProvider);
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
                await _commandService.ExecuteAsync(context, "share", _scopeFactory.CreateScope().ServiceProvider);
                return;
            }

            int argPos = 0;
            if (!message.HasStringPrefix(_botOptions.Prefix, ref argPos) && !message.HasMentionPrefix(Client.CurrentUser, ref argPos)) return;

            await _commandService.ExecuteAsync(context, argPos, _scopeFactory.CreateScope().ServiceProvider);
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
