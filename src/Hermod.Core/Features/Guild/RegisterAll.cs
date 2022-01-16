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
            public List<ulong> GuildIds { get; init; } = new();
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly HermodContext _hermodContext;
            private readonly ILogger<Handler> _logger;
            
            public Handler(HermodContext hermodContext, ILogger<Handler> logger)
            {
                _hermodContext = hermodContext;
                _logger = logger;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var registeredGuilds = await _hermodContext
                    .Guilds
                    .Select(g => g.GuildId)
                    .ToListAsync(cancellationToken);

                var guildsToAdd = request
                    .GuildIds
                    .Except(registeredGuilds)
                    .Select(g => new Data.Models.Guild
                    {
                        GuildId = g,
                        PostChannelId = GetPostChannel(g),
                        AllowSharing = true
                    });

                if (guildsToAdd.Any())
                {
                    _hermodContext.Guilds.AddRange(guildsToAdd);
                    await _hermodContext.SaveChangesAsync(CancellationToken.None);
                }

                return default;
            }
            private ulong? GetPostChannel(ulong guild) => guild switch
            {
                196095053154746369 => 919038735188897792,
                932103115761647626 => 932103115761647629,
                _ => null
            };
        }


        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleForEach(command => command.GuildIds).NotEmpty();
            }
        }
    }
}
