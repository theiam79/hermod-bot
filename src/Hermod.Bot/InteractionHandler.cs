using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Hosting.Util;

namespace Hermod.Bot
{
    internal class InteractionHandler : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly InteractionService _interactionService;

        public InteractionHandler(
            DiscordSocketClient client,
            ILogger<InteractionHandler> logger,
            IServiceProvider serviceProvider,
            InteractionService interactionService
            ) : base(client, logger)
        {
            _serviceProvider = serviceProvider;
            _interactionService = interactionService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Searching for modules to load");
            await _interactionService.AddModuleAsync<Modules.Guild.Module>(_serviceProvider);

            Client.InteractionCreated += HandleInteraction;

            _interactionService.SlashCommandExecuted += SlashCommandExecuted;
            await Client.WaitForReadyAsync(stoppingToken);

            await _interactionService.RegisterCommandsToGuildAsync(196095053154746369);
        }
        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(Client, arg);
                await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // implement
                        break;
                    case InteractionCommandError.BadArgs:
                        // implement
                        break;
                    case InteractionCommandError.Exception:
                        // implement
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // implement
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }

    }
}
