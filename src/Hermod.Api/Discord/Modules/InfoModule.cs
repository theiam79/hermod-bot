using Discord.Interactions;
using Hermod.Data;

namespace Hermod.Api.Discord.Modules;

[Group("hermod", "Hermod bot commands")]
public partial class InfoModule(HermodContext db) : InteractionModuleBase<SocketInteractionContext>
{
    private readonly HermodContext _db = db;

    [SlashCommand("ping", "Check if the bot is alive")]
    public async Task PingAsync() => await RespondAsync("Pong!");
}
