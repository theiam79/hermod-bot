using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Core.Features.Collection
{
    public class Register
    {
        public record Command : IRequest
        {
            public ulong DiscordId { get; init; }
            public int BggId { get; init; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly ILogger<Handler> _logger;

            public Handler(ILogger<Handler> logger)
            {
                _logger = logger;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                _logger.LogDebug("Registering collection {@Command}", request);
                return default;
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {

            }
        }
    }
}
