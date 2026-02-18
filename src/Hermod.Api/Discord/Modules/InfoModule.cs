using Discord.Interactions;

namespace Hermod.Api.Discord.Modules;

[Group("hermod", "Hermod bot commands")]
public class InfoModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Check if the bot is alive")]
    public async Task PingAsync() => await RespondAsync("Pong!");
}
