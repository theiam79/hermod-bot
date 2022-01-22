using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentResults;
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
    public class Edit
    {
        public record class Query : IRequest<Result<Command>>
        {
            public ulong GuildId { get; init; }
        }

        public record class Command : IRequest<Result>
        {
            public ulong GuildId { get; set; }
            //public ulong? ManagementRole { get; set; }
            public ulong PostChannelId { get; set; }
            public bool AllowSharing { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.GuildId).NotEmpty();
                RuleFor(x => x.PostChannelId).NotEmpty();
            }
        }

        public class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<Command, Data.Models.Guild>()
                    .ReverseMap();
            }
        }

        public class QueryHandler : IRequestHandler<Query, Result<Command>>
        {
            private readonly HermodContext _context;
            private readonly IConfigurationProvider _configurationProvider;
            public QueryHandler(HermodContext context, IConfigurationProvider configurationProvider)
            {
                _context = context;
                _configurationProvider = configurationProvider;
            }

            public async Task<Result<Command>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await Result.Try(() => _context
                    .Guilds
                    .Where(g => g.GuildId == request.GuildId)
                    .ProjectTo<Command>(_configurationProvider)
                    .SingleAsync());
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly HermodContext _hermodContext;
            private readonly IMapper _mapper;
            private readonly ILogger<Handler> _logger;

            public Handler(HermodContext hermodContext, IMapper mapper, ILogger<Handler> logger)
            {
                _hermodContext = hermodContext;
                _mapper = mapper;
                _logger = logger;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                if (await _hermodContext.Guilds.FirstOrDefaultAsync(g => g.GuildId == request.GuildId, cancellationToken) is not Data.Models.Guild guild)
                {
                    return Result.Fail("That guild is not registered");
                }

                _mapper.Map(request, guild);
                await _hermodContext.SaveChangesAsync(CancellationToken.None);

                return Result.Ok();
            }
        }
    }
}
