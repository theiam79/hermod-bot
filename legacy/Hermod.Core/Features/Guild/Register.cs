using Discord.Interactions;
using FluentValidation;
using Hermod.Data.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Core.Features.Guild
{
    public class Register
    {
        public class Command : IRequest
        {
            public ulong GuildId { get; init; }
            public ulong ChannelId { get; init; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly HermodContext _hermodContext;
            private readonly InteractionService _interactionService;

            public Handler(HermodContext hermodContext, InteractionService interactionService)
            {
                _hermodContext = hermodContext;
                _interactionService = interactionService;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                if (await _hermodContext.Guilds.AnyAsync(g => g.GuildId == request.GuildId, cancellationToken))
                {
                    return default;
                }

                var guild = new Data.Models.Guild
                {
                    GuildId = request.GuildId,
                    PostChannelId = request.ChannelId
                };

                _hermodContext.Guilds.Add(guild);
                await _hermodContext.SaveChangesAsync(CancellationToken.None);
                await _interactionService.RegisterCommandsToGuildAsync(request.GuildId);
                return default;
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(command => command.GuildId).NotEmpty();
            }
        }
    }
}
