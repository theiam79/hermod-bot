using Discord.Addons.Hosting;
using Discord.WebSocket;
using MediatR;

namespace Hermod.Bot
{
    internal class GuildHandler : DiscordClientService
    {
        private readonly IMediator _mediator;
        public GuildHandler(IMediator mediator,
                            DiscordSocketClient client,
                            ILogger<GuildHandler> logger) : base(client, logger)
        {
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.JoinedGuild += HandleJoinedGuild;

            var command = new Core.Features.Guild.RegisterAll.Command
            {
                GuildIds = Client.Guilds.Select(g => g.Id).ToList()
            };

            await _mediator.Send(command, stoppingToken);
        }

        private async Task HandleJoinedGuild(SocketGuild arg)
        {
            var command = new Core.Features.Guild.Register.Command { GuildId = arg.Id };
            await _mediator.Send(command);
        }
    }
}
