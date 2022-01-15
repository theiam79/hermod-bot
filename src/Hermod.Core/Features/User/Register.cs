using Bgg.Sdk;
using Discord;
using Discord.WebSocket;
using FluentValidation;
using Hermod.Data.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Core.Features.User
{
    public class Register
    {
        public class Command : IRequest
        {
            public ulong UserId { get; init; }
            public string BggUsername { get; init; } = "";
            public ulong GuildId { get; init; }
            public SubscibeOption SubscibeOption { get; init; }
        }

        public enum SubscibeOption
        {
            None = 0,
            All = 1,
            ThisGuild = 2
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly HermodContext _hermodContext;
            private readonly IBggClient _bggClient;
            private readonly DiscordSocketClient _discordClient;

            public Handler(HermodContext hermodContext, IBggClient bggClient, DiscordSocketClient discordClient)
            {
                _hermodContext = hermodContext;
                _bggClient = bggClient;
                _discordClient = discordClient;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var normalizedBggUsername = request.BggUsername.ToUpper();
                var userAlreadyRegistered = await _hermodContext
                    .Users
                    .Where(u => u.DiscordId == request.UserId && u.NormalizedBggUsername == normalizedBggUsername)
                    .AnyAsync();

                if (userAlreadyRegistered)
                {
                    return default;
                }

                var query = new Bgg.Sdk.Core.User.QueryParameters(request.BggUsername);
                if ((await _bggClient.UserAsync(query)) is not Bgg.Sdk.Models.User bggUser)
                {
                    return default;
                }

                var guildsToRegister = _discordClient
                    .Guilds
                    .SelectMany(g => g.Users.Where(u => u.Id == request.UserId).Take(1))
                    .Select(gu => new Data.Models.UserGuild
                    {
                        GuildId = gu.Guild.Id,
                        UserNickname = gu.Nickname,
                        SubscribeToPlays = true,
                    })
                    .ToList();

                var user = new Data.Models.User
                {
                    DiscordId = request.UserId,
                    BggUsername = request.BggUsername,
                    NormalizedBggUsername = normalizedBggUsername,
                    BggId = bggUser.Id,
                    UserGuilds = guildsToRegister
                };

                _hermodContext.Users.Add(user);
                await _hermodContext.SaveChangesAsync(CancellationToken.None);

                return default;
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(command => command.UserId).NotEmpty();
                RuleFor(command => command.BggUsername).NotEmpty();
                RuleFor(command => command.GuildId).NotEmpty()
                    .When(command => command.SubscibeOption == SubscibeOption.ThisGuild)
                    .WithMessage("This guild option requires a guildId to be included in the request");
            }
        }
    }
}
