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
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly HermodContext _hermodContext;

            public Handler(HermodContext hermodContext)
            {
                _hermodContext = hermodContext;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                if (await _hermodContext.Guilds.AnyAsync(g => g.GuildId == request.GuildId, cancellationToken))
                {
                    return default;
                }

                var guild = new Data.Models.Guild
                {
                    GuildId = request.GuildId
                };

                _hermodContext.Guilds.Add(guild);
                await _hermodContext.SaveChangesAsync(CancellationToken.None);
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
