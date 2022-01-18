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
using FluentResults;
using System.Text.Json;
using Hermod.BGstats;

namespace Hermod.Core.Features.Share
{
    public class Notify
    {
        public class Command : IRequest<Result>
        {
            public ulong Guild { get; init; }
            public ulong Sender { get; init; }
            public Discord.Attachment? Attachment { get; init; }
            public Discord.Attachment? PlayFile { get; init; }
            public List<string> Players { get; init; } = new();
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly HermodContext _hermodContext;
            private readonly HttpClient _httpClient;
            private readonly DiscordSocketClient _discordSocketClient;

            public Handler(HermodContext hermodContext, HttpClient httpClient, DiscordSocketClient discordSocketClient)
            {
                _hermodContext = hermodContext;
                _httpClient = httpClient;
                _discordSocketClient = discordSocketClient;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                jsonOptions.Converters.Add(new DateTimeConverter());
                jsonOptions.Converters.Add(new IntToBoolConverter());

                var stream = await _httpClient.GetStreamAsync(request.PlayFile!.Url);

                if (await JsonSerializer.DeserializeAsync<PlayFile>(stream, jsonOptions) is not PlayFile playFile)
                {
                    return Result.Fail("Unable to parse the supplied .bgsplay file");
                }
                
                var players = playFile.Players.Select(p => p.BggUsername.Trim().ToUpper()).ToList();

                var recipients = await _hermodContext
                    .UserGuilds
                    .Where(ug => ug.Guild.GuildId == request.Guild)
                    .Where(ug => ug.SubscribeToPlays)
                    .Where(ug => request.Players.Contains(ug.User.NormalizedBggUsername))
                    .Where(ug => ug.User.DiscordId != request.Sender)
                    .Select(ug => ug.User.DiscordId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (!recipients.Any())
                {
                    return Result.Ok().WithSuccess("Found no registered players to notify");
                }

                var sourceStream = await _httpClient.GetStreamAsync(request.Attachment!.Url, cancellationToken);
                var memoryStream = new MemoryStream();
                await sourceStream.CopyToAsync(memoryStream, cancellationToken);

                var client = new DiscordSocketClient();
                List<Result> results = new(recipients.Count);
                foreach (var user in recipients)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    results.Add(await ShareWithPlayer(user, memoryStream));
                }

                return Result.Merge(results.ToArray());
            }

            private async Task<Result> ShareWithPlayer(ulong userId, Stream stream)
            {
                if (await _discordSocketClient.GetUserAsync(userId) is not Discord.IUser user)
                {
                    return Result.Fail($"Could not find user with ID {userId}");
                }

                if (await user.CreateDMChannelAsync() is not Discord.IDMChannel channel)
                {
                    return Result.Fail($"Failed to open DM channel with {user.Username}");
                }

                await channel.SendFileAsync(stream, "A play was shared that included your BGG username");
                return Result.Ok().WithSuccess($"Shared with {user.Username}");
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
