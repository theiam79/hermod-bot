using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Hermod.Bot.Modules
{
    public class User : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMediator _mediator;

        public User(IMediator mediator)
        {
            _mediator = mediator;
        }

        [SlashCommand("register", "register your bgg username, giving you access to play alerts and making your collection searchable")]
        public async Task RegisterCollectionAsync(string bggUsername)
        {
            await DeferAsync(ephemeral: true);

            var command = new Core.Features.User.Register.Command
            {
                DiscordId = Context.User.Id,
                BggUsername = bggUsername
            };

            await _mediator.Send(command);
            await FollowupAsync($"Successfully registered {bggUsername}");
        }
    }
}
