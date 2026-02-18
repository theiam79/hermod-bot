using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Discord;

public class InteractionHandler(
    DiscordSocketClient client,
    ILogger<InteractionHandler> logger,
    InteractionService interactionService,
    IServiceProvider services)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Ready += OnReadyAsync;
        Client.InteractionCreated += OnInteractionCreatedAsync;
        interactionService.SlashCommandExecuted += OnSlashCommandExecutedAsync;

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnReadyAsync()
    {
        try
        {
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            await interactionService.RegisterCommandsGloballyAsync();
            Logger.LogInformation("Registered {Count} slash command module(s) globally",
                interactionService.Modules.Count());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register slash commands globally");
        }
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(Client, interaction);
            await interactionService.ExecuteCommandAsync(ctx, services);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling interaction {InteractionId}", interaction.Id);
            
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                try
                {
                    await interaction.RespondAsync(
                        "An error occurred while executing the command.", ephemeral: true);
                }
                catch
                {
                    // Already responded — swallow; the error is already logged above
                }
            }
        }
    }

    private Task OnSlashCommandExecutedAsync(
        SlashCommandInfo command,
        IInteractionContext context,
        global::Discord.Interactions.IResult result)
    {
        if (!result.IsSuccess)
        {
            Logger.LogWarning("Slash command /{Name} failed: {Error}", command.Name, result.ErrorReason);
        }

        return Task.CompletedTask;
    }
}
