using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hermod.Data.Context;
using Microsoft.EntityFrameworkCore;
using Discord.WebSocket;

namespace Hermod.Core.Features.Share
{
    public class Notify
    {
        public class Command : IRequest
        {
            public ulong Guild { get; init; }
            public ulong Sender { get; init; }
            public Discord.Attachment? Attachment { get; init; }
            public List<string> Players { get; init; } = new();
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly HermodContext _hermodContext;
            private readonly HttpClient _httpClient;

            public Handler(HermodContext hermodContext, HttpClient httpClient)
            {
                _hermodContext = hermodContext;
                _httpClient = httpClient;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                //var recipients = await _hermodContext
                //    .UserGuilds
                //    .Where(ug => ug.Guild.GuildId == request.Guild)
                //    .Where(ug => ug.SubscribeToPlays)
                //    .Where(ug => request.Players.Contains(ug.User.BggUsername))
                //    .Where(ug => ug.User.DiscordId != request.Sender)
                //    .Select(ug => ug.User.DiscordId)
                //    .Distinct()
                //    .ToListAsync(cancellationToken);

                //if (!recipients.Any())
                //{
                //    return default;
                //}

                //var sourceStream = await _httpClient.GetStreamAsync(request.Attachment!.Url);
                //var memoryStream = new MemoryStream();
                //await sourceStream.CopyToAsync(memoryStream);

                //var client = new DiscordSocketClient();
                //foreach (var user in recipients)
                //{
                //    memoryStream.Seek(0, SeekOrigin.Begin);
                //    if (await client.GetUserAsync(user) is Discord.IUser found 
                //        && await found.CreateDMChannelAsync() is Discord.IDMChannel channel)
                //    {
                //        await channel.SendFileAsync(memoryStream, "A play was shared that included your BGG username");
                //    }
                //}

                return default;
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(command => command.Guild).NotEmpty();
                RuleFor(command => command.Attachment).NotEmpty();
                RuleFor(command => command.Sender).NotEmpty();
                RuleFor(command => command.Players).NotEmpty();
            }
        }
    }
}
