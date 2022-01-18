using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
//using Core as Hermod.Core.Features;
using Collection = Hermod.Core.Features.Collection;

namespace Hermod.Bot.Modules.Collection
{
    internal class Collection : ModuleBase<SocketCommandContext>
    {
        private readonly IMediator _mediator;

        public Collection(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Command("collection")]
        public async Task RegisterCollectionAsync(string bggUsername)
        {
            var guildUser = Context.Guild.GetUser(Context.User.Id);
            var command = new Core.Features.Collection.Register.Command
            {
                DiscordId = guildUser.Id,
                GuildId = Context.Guild.Id,
                BggUsername = bggUsername
            };

            await _mediator.Send(command);
        }
    }
}
