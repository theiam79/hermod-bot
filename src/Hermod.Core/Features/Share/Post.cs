using Discord;
using Discord.WebSocket;
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
        public class Command : IRequest
        {
            public ulong Guild { get; init; }
            public ulong CommandChannel { get; init; }
            public Attachment? PlayFile { get; init; }
            public string? ImageUrl { get; init; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly HermodContext _hermodContext;
            private readonly HttpClient _httpClient;
            private readonly ILogger<Handler> _logger;

            public Handler(HermodContext hermodContext, HttpClient httpClient, ILogger<Handler> logger)
            {
                _hermodContext = hermodContext;
                _httpClient = httpClient;
                _logger = logger;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var channelId = await _hermodContext
                    .Guilds
                    .Where(g => g.GuildId == request.Guild)
                    .Select(g => g.PostChannelId)
                    .FirstOrDefaultAsync();

                if (channelId == default)
                {
                    _logger.LogDebug("Post channel not set for {Guild}, using command channel", request.Guild);
                    channelId = request.CommandChannel;
                }

                var client = new DiscordSocketClient();

                if (client.GetGuild(request.Guild)?.GetTextChannel(channelId) is not SocketTextChannel channel)
                {
                    _logger.LogDebug("Failed to get {Channel} from {Guild}", channelId, request.Guild);
                    return default;
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
                    return default;
                }

                var play = playFile.MapToPlay();

                var embedBuilder = FormatPlay(play);
                if (request.ImageUrl != null)
                {
                    embedBuilder.WithImageUrl(request.ImageUrl);
                }

                await channel.SendMessageAsync(embed: embedBuilder.Build());

                return default;
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
