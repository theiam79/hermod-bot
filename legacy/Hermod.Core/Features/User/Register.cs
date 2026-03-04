using Bgg.Sdk;
using Discord;
using Discord.WebSocket;
using FluentResults;
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
        public record class Command : IRequest<Result>
        {
            public ulong DiscordId { get; init; }
            public string BggUsername { get; init; } = "";
        }

        public class Handler : IRequestHandler<Command, Result>
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

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var normalizedBggUsername = request.BggUsername.ToUpper();
                var userAlreadyRegistered = await _hermodContext
                    .Users
                    .Where(u => u.DiscordId == request.DiscordId && u.NormalizedBggUsername == normalizedBggUsername)
                    .AnyAsync();

                if (userAlreadyRegistered)
                {
                    return Result.Ok().WithSuccess("User already registered");
                }

                var query = new Bgg.Sdk.Core.User.QueryParameters(request.BggUsername);
                if ((await _bggClient.UserAsync(query)) is not Bgg.Sdk.Models.User bggUser)
                {
                    return Result.Fail($"Could not find BGG user with username: {request.BggUsername}");
                }

                var guildsUserIsIn = _discordClient
                    .Guilds
                    .SelectMany(g => g.Users.Where(u => u.Id == request.DiscordId).Take(1))
                    .Select(gu => new Data.Models.UserGuild
                    {
                        GuildId = gu.Guild.Id,
                        UserNickname = gu.Nickname,
                    })
                    .ToList();

                var user = new Data.Models.User
                {
                    DiscordId = request.DiscordId,
                    BggUsername = request.BggUsername,
                    NormalizedBggUsername = normalizedBggUsername,
                    BggId = bggUser.Id,
                    SubscribeToPlays = true,
                    UserGuilds = guildsUserIsIn
                };

                _hermodContext.Users.Add(user);
                await _hermodContext.SaveChangesAsync(CancellationToken.None);

                return Result.Ok();
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(command => command.DiscordId).NotEmpty();
                RuleFor(command => command.BggUsername).NotEmpty();
            }
        }
    }
}
