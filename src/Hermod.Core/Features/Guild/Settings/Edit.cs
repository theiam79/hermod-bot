using AutoMapper;
using AutoMapper.EntityFrameworkCore;
using AutoMapper.EquivalencyExpression;
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

namespace Hermod.Core.Features.Guild.Settings
{
    public class Edit
    {
        public class Command : IRequest<Result>
        {
            public ulong GuildId { get; set; }
            public ulong? ManagementRole { get; set; }
            public ulong? PostChannel { get; set; }
            public bool? Sharing { get; set; }
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
                var allGuilds = await _hermodContext.Guilds.ToListAsync();
                if (await _hermodContext.Guilds.FirstOrDefaultAsync(g => g.GuildId == request.GuildId, cancellationToken) is not Data.Models.Guild guild)
                {
                    return Result.Fail("That guild is not registered");
                }

                _mapper.Map(request, guild);
                await _hermodContext.SaveChangesAsync(CancellationToken.None);

                return Result.Ok();
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.GuildId).NotEmpty();
                RuleFor(command => command)
                    .Must(command => command.ManagementRole.HasValue || command.PostChannel.HasValue || command.Sharing.HasValue)
                    .WithMessage("At least one setting needs to be set");
            }
        }

        public class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<Command, Data.Models.Guild>()
                    .ForMember(dest => dest.ManagementRole, opt =>
                    {
                        opt.PreCondition(src => src.ManagementRole != null);
                        opt.MapFrom(src => src.ManagementRole!.Value);
                    })
                    .ForMember(dest => dest.PostChannelId, opt =>
                    {
                        opt.PreCondition(src => src.PostChannel != null);
                        opt.MapFrom(src => src.PostChannel!.Value);
                    })
                    .ForMember(dest => dest.AllowSharing, opt =>
                    {
                        opt.PreCondition(src => src.Sharing != null);
                        opt.MapFrom(src => src.Sharing!.Value);
                    })
                    .EqualityComparison((src, dest) => src.GuildId == dest.GuildId);
            }
        }
    }
}
