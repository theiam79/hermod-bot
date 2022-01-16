using Discord;
using Discord.WebSocket;
using FluentResults;
using FluentValidation;
using Hermod.BGstats;
using Hermod.Data.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hermod.Core.Features.Share
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public ulong Sender { get; init; }
            public ulong? Guild { get; init; }
            public ulong CommandChannel { get; init; }
            public Attachment? PlayFile { get; init; }
            public string? ImageUrl { get; init; }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly HermodContext _hermodContext;
            private readonly HttpClient _httpClient;
            private readonly ILogger<Handler> _logger;
            private readonly DiscordSocketClient _discordSocketClient;

            public Handler(HermodContext hermodContext, HttpClient httpClient, ILogger<Handler> logger, DiscordSocketClient discordSocketClient)
            {
                _hermodContext = hermodContext;
                _httpClient = httpClient;
                _logger = logger;
                _discordSocketClient = discordSocketClient;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                List<Target> targets;

                if (request.Guild == null)
                {
                    targets = await _hermodContext
                        .Guilds
                        .Where(g => request.Guild == default || g.GuildId == request.Guild!.Value)
                        .Where(g => g.AllowSharing)
                        .Where(g => g.PostChannelId != default)
                        .Where(g => g.Users.Any(u => u.DiscordId == request.Sender))
                        .Select(g => new Target(g.GuildId, g.PostChannelId!.Value))
                        .Distinct()
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    targets = await _hermodContext
                        .Guilds
                        .Where(g => g.GuildId == request.Guild.Value)
                        .Select(g => new Target(g.GuildId, g.PostChannelId ?? request.CommandChannel))
                        .ToListAsync();
                }

                if (!targets.Any())
                {
                    return Result.Fail("No target guild(s) found");
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                jsonOptions.Converters.Add(new DateTimeConverter());
                jsonOptions.Converters.Add(new IntToBoolConverter());

                var stream = await _httpClient.GetStreamAsync(request.PlayFile!.Url);

                if (await JsonSerializer.DeserializeAsync<PlayFile>(stream, jsonOptions) is not PlayFile playFile)
                {
                    _logger.LogDebug("Failed to deserialize {File} from {FileUrl}", request.PlayFile.Filename, request.PlayFile.Url);
                    return Result.Fail("Unable to parse the supplied .bgsplay file");
                }

                var play = playFile.MapToPlay();

                var embedBuilder = FormatPlay(play);
                if (request.ImageUrl != null)
                {
                    embedBuilder.WithImageUrl(request.ImageUrl);
                }
                var embed = embedBuilder.Build();

                var results = await Task.WhenAll(targets.Select(pt => PostPlay(pt.Guild, pt.Channel, embed)));
                return Result.Merge(results);
            }

            private class Target
            {
                public Target(ulong guild, ulong channel)
                {
                    Guild = guild;
                    Channel = channel;
                }

                public ulong Guild { get; }
                public ulong Channel { get; }
            }

            private async Task<Result> PostPlay(ulong guildId, ulong channelId, Embed embed)
            {
                if (_discordSocketClient.GetGuild(guildId) is not SocketGuild guild)
                {
                    return Result.Fail($"Couldn't retrieve guild with ID {guildId}");
                }

                if (guild.GetTextChannel(channelId) is not SocketTextChannel channel)
                {
                    return Result.Fail($"Could not find the configured post channel for guild: {guild.Name}");
                }

                await channel.SendMessageAsync(embed: embed);
                return Result.Ok().WithSuccess($"Successfully posted to {guild.Name}");
            }

            private EmbedBuilder FormatPlay(Play play)
            {
                return new EmbedBuilder()
                    .WithTitle(play.Game.Name)
                    .WithDescription(BuildDescription(play))
                    .WithFooter(BuildFooter(play))
                    .WithThumbnailUrl(play.Game.ThumbnailUrl)
                    .WithTimestamp(play.DatePlayed)
                    .WithColor(Color.Green)
                    .WithFields(ScoreFields(play))
                    ;
            }

            private IEnumerable<EmbedFieldBuilder> ScoreFields(Play play)
            {
                return play.UsesTeams ? TeamScores(play) : PlayerScores(play);
            }

            private IEnumerable<EmbedFieldBuilder> TeamScores(Play play)
            {
                var teams = play.Scores.GroupBy(s => s.Team).OrderBy(t => t.Key);

                foreach (var team in teams)
                {
                    var teamName = int.TryParse(team.Key, out var parsed) ? $"Team {++parsed}" : team.Key;
                    var teamTitle = $"\r\n{teamName}{(team.Any(s => s.Winner) ? " :trophy:" : "")}";
                    var sb = new StringBuilder();

                    foreach (var teamScore in team)
                    {
                        sb.Append(teamScore.Player.Name);
                        var calculatedScore = teamScore.CalculateScore();
                        if (calculatedScore != default)
                        {
                            sb.Append($" - {calculatedScore}");
                        }
                        sb.AppendLine();
                        sb.Append("```");
                        if (!string.IsNullOrEmpty(teamScore.Role)) { sb.AppendLine($"Role: {teamScore.Role}"); }
                        sb.Append($"BGG: {(string.IsNullOrWhiteSpace(teamScore.Player.BggUsername) ? "Not set" : teamScore.Player.BggUsername)}```");
                    }

                    yield return new EmbedFieldBuilder()
                        .WithName(teamTitle)
                        .WithValue(sb.ToString())
                        .WithIsInline(false);
                }
            }

            private IEnumerable<EmbedFieldBuilder> PlayerScores(Play play)
            {
                var sb = new StringBuilder();

                foreach (var score in play.Scores)
                {
                    sb.Append(score.Player.Name);
                    var calculatedScore = score.CalculateScore();
                    if (calculatedScore != default)
                    {
                        sb.Append($" - {calculatedScore}");
                    }
                    if (score.Winner) { sb.Append(" :trophy: "); }
                    sb.AppendLine();

                    sb.Append("```");
                    if (!string.IsNullOrEmpty(score.Role)) { sb.AppendLine($"Role: {score.Role}"); }
                    sb.Append($"BGG: {(string.IsNullOrEmpty(score.Player.BggUsername) ? "Not set" : score.Player.BggUsername)}```");
                }

                yield return new EmbedFieldBuilder()
                    .WithName("Players")
                    .WithValue(sb.ToString())
                    .WithIsInline(false)
                    ;
            }

            private string BuildDescription(Play play)
            {
                var descriptionItems = new List<string>
            {
                play.Location.Name
            };

                if (play.Rounds != 0)
                {
                    var roundDescription = play.Rounds == 1 ? "1 Round" : $"{play.Rounds} Rounds";
                    descriptionItems.Add(roundDescription);
                }

                var hourDescription = play.Duration switch
                {
                    { Hours: 0 } => null,
                    { Hours: 1 } => "1 hour",
                    { Hours: var h } => $"{h} hours"
                };

                var minuteDescription = play.Duration switch
                {
                    { Minutes: 0 } => null,
                    { Minutes: 1 } => "1 minute",
                    { Minutes: var m } => $"{m} minutes"
                };

                var durationDescription = (hourDescription, minuteDescription) switch
                {
                    (not null, not null) => $"{hourDescription} and {minuteDescription}",
                    (not null, null) => hourDescription,
                    (null, not null) => minuteDescription,
                    _ => "Untimed"
                };

                descriptionItems.Add(durationDescription);

                if (play.IgnoredForStatistics)
                {
                    descriptionItems.Add(Format.Bold("Ignored for stats"));
                }

                var shareLink = play.CreateShareLink();
                if (!string.IsNullOrWhiteSpace(shareLink))
                {
                    descriptionItems.Add(Format.Url("Import", shareLink));
                }

                var sb = new StringBuilder();
                return sb.AppendJoin(" - ", descriptionItems).ToString();
            }

            private string BuildFooter(Play play)
            {
                var footerItems = new List<string>(3)
            {
                $"{play.Game.BggYear} {play.Game.Designers}"
            };

                var style = play switch
                {
                    { Game.Cooperative: true } => "Co-op",
                    { UsesTeams: true } => "PvP (Teams)",
                    _ => "PvP"
                };

                footerItems.Add(style);
                footerItems.Add($"{play.Game.MinPlayerCount}-{play.Game.MaxPlayerCount} Players");

                var sb = new StringBuilder();
                return sb.AppendJoin(" | ", footerItems).ToString();
            }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(command => command.Guild).NotEmpty();
                RuleFor(command => command.CommandChannel).NotEmpty();
                RuleFor(command => command.PlayFile).NotEmpty()
                    .Must(a => a!.Filename.EndsWith(".bgsplay", StringComparison.InvariantCultureIgnoreCase))
                    .WithMessage("Invalid extension for play file attachment");
            }
        }
    }
}
