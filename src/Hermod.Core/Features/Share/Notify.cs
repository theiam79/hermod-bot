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
using Discord;

namespace Hermod.Core.Features.Share
{
    public class Notify
    {
        public class Command : IRequest<Result>
        {
            public Command(ulong sender, Attachment playFile)
            {
                Sender = sender;
                PlayFile = playFile;
            }

            public ulong Sender { get; init; }
            public Attachment PlayFile { get; init; }
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

                var stream = await _httpClient.GetStreamAsync(request.PlayFile.Url, cancellationToken);

                if (await JsonSerializer.DeserializeAsync<PlayFile>(stream, jsonOptions, cancellationToken) is not PlayFile playFile)
                {
                    return Result.Fail("Unable to parse the supplied .bgsplay file");
                }
                
                var players = playFile.Players.Select(p => p.BggUsername.Trim().ToUpper()).ToList();

                var recipients = await _hermodContext
                    .Users
                    .Where(u => u.SubscribeToPlays)
                    .Where(u => u.DiscordId != request.Sender)
                    .Where(u => players.Contains(u.NormalizedBggUsername))
                    //.IntersectBy(players, u => u.NormalizedBggUsername)
                    .Select(u => u.DiscordId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (!recipients.Any())
                {
                    return Result.Ok().WithSuccess("Found no registered players to notify");
                }

                var sourceStream = await _httpClient.GetStreamAsync(request.PlayFile.Url, cancellationToken);
                var primaryStream = new MemoryStream();
                await sourceStream.CopyToAsync(primaryStream, cancellationToken);

                var client = new DiscordSocketClient();
                List<Result> results = new(recipients.Count);
                foreach (var user in recipients)
                {
                    primaryStream.Seek(0, SeekOrigin.Begin);

                    //SendFileAsync disposes of the stream so we have to make a copy of the copy
                    using var copiedStream = new MemoryStream();
                    await primaryStream.CopyToAsync(copiedStream, cancellationToken);
                    copiedStream.Seek(0, SeekOrigin.Begin);

                    results.Add(await ShareWithPlayer(user, request.PlayFile.Filename, copiedStream));
                }

                return Result.Merge(results.ToArray());
            }

            private async Task<Result> ShareWithPlayer(ulong userId, string fileName, Stream stream)
            {
                if (await _discordSocketClient.GetUserAsync(userId) is not IUser user)
                {
                    return Result.Fail($"Could not find user with ID {userId}");
                }

                if (await user.CreateDMChannelAsync() is not IDMChannel channel)
                {
                    return Result.Fail($"Failed to open DM channel with {user.Username}");
                }

                await channel.SendFileAsync(stream, fileName);
                return Result.Ok().WithSuccess($"Shared with {user.Username}");
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(command => command.Sender).NotEmpty();
                RuleFor(command => command.PlayFile).NotEmpty();
            }
        }
    }
}
