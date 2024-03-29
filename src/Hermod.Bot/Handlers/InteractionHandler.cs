﻿using Discord;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly InteractionService _interactionService;

        public InteractionHandler(
            DiscordSocketClient client,
            ILogger<InteractionHandler> logger,
            IServiceScopeFactory scopeFactory,
            InteractionService interactionService
            ) : base(client, logger)
        {
            _scopeFactory = scopeFactory;
            _interactionService = interactionService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Searching for modules to load");

            var serviceProvider = _scopeFactory.CreateScope().ServiceProvider;
            await _interactionService.AddModuleAsync<Modules.Guild>(serviceProvider);
            await _interactionService.AddModuleAsync<Modules.Info>(serviceProvider);
            await _interactionService.AddModuleAsync<Modules.User>(serviceProvider);
            await _interactionService.AddModuleAsync<Modules.CollectionModule>(serviceProvider);

            Client.InteractionCreated += HandleInteraction;

            _interactionService.SlashCommandExecuted += SlashCommandExecuted;
            await Client.WaitForReadyAsync(stoppingToken);

            //foreach (var guild in Client.Guilds)
            //{
            //    await _interactionService.RegisterCommandsToGuildAsync(guild.Id);
            //}

            //await _interactionService.RegisterCommandsToGuildAsync(932103115761647626);
            //await _interactionService.RegisterCommandsToGuildAsync(196095053154746369);
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(Client, arg);
                await _interactionService.ExecuteCommandAsync(ctx, _scopeFactory.CreateScope().ServiceProvider);
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
