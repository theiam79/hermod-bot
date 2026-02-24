using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Hermod.Bot.Services;

public class InteractionHandler(
    DiscordSocketClient client,
    ILogger<InteractionHandler> logger,
    InteractionService interactions,
    IServiceProvider services) : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        await Client.WaitForReadyAsync(stoppingToken);

        await interactions.RegisterCommandsGloballyAsync();
        logger.LogInformation("Registered {Count} slash command(s) globally", interactions.SlashCommands.Count);

        Client.InteractionCreated += OnInteractionCreated;
    }

    private async Task OnInteractionCreated(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(Client, interaction);
            await interactions.ExecuteCommandAsync(ctx, services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling interaction {InteractionId}", interaction.Id);

            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(msg => msg.Result.DeleteAsync());
            }
        }
    }
}
