using Discord.Interactions;
using FluentValidation;
using Hermod.Data.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Core.Features.Guild
{
    public class RegisterAll
    {
        public class Command  : IRequest
        {
            public List<(ulong GuildId, ulong ChannelId)> Guilds { get; init; } = new();
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly HermodContext _hermodContext;
            private readonly InteractionService _interactionService;
            private readonly ILogger<Handler> _logger;
            
            public Handler(HermodContext hermodContext, InteractionService interactionService, ILogger<Handler> logger)
            {
                _hermodContext = hermodContext;
                _interactionService = interactionService;
                _logger = logger;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var registeredGuilds = await _hermodContext
                    .Guilds
                    .Select(g => g.GuildId)
                    .ToListAsync(cancellationToken);

                var guildsToAdd = request
                    .Guilds
                    .ExceptBy(registeredGuilds, x => x.GuildId)
                    .Select(g => new Data.Models.Guild
                    {
                        GuildId = g.GuildId,
                        PostChannelId = g.ChannelId,
                        AllowSharing = true
                    });

                if (guildsToAdd.Any())
                {
                    _hermodContext.Guilds.AddRange(guildsToAdd);
                    await _hermodContext.SaveChangesAsync(CancellationToken.None);
                }

                foreach (var guild in request.Guilds.Select(g => g.GuildId))
                {
                    await _interactionService.RegisterCommandsToGuildAsync(guild);
                }

                return default;
            }
        }


        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleForEach(command => command.Guilds).ChildRules(guild =>
                {
                    guild.RuleFor(g => g.GuildId).NotEmpty();
                    guild.RuleFor(g => g.ChannelId).NotEmpty();
                });
            }
        }
    }
}
