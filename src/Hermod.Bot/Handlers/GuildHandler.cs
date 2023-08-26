using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using MediatR;

namespace Hermod.Bot
{
    internal class GuildHandler : DiscordClientService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public GuildHandler(IServiceScopeFactory scopeFactory,
                            DiscordSocketClient client,
                            ILogger<GuildHandler> logger
                            ) : base(client, logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.JoinedGuild += HandleJoinedGuild;

            await Client.WaitForReadyAsync(stoppingToken);

            var command = new Core.Features.Guild.RegisterAll.Command
            {
                //default channel isn't reliable, need to use own logic
                Guilds = Client.Guilds.Select(g => (g.Id, g.DefaultChannel.Id)).ToList()
            };

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(command, stoppingToken);
        }

        private async Task HandleJoinedGuild(SocketGuild arg)
        {
            var command = new Core.Features.Guild.Register.Command { GuildId = arg.Id };
            
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(command);
        }
    }
}
