using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hermod.BGstats;
using Discord;
using MediatR;

namespace Hermod.Bot.Modules.Share
{
    internal class Module : ModuleBase<SocketCommandContext>
    {
        private readonly IMediator _mediator;
        private readonly List<string> _photoExtensions;

        public Module(IMediator mediator)
        {
            _mediator = mediator;

            _photoExtensions = new List<string>()
            {
                "jpg",
                "jpeg",
                "png"
            };
        }

        [Command("share")]
        [Summary("Shares a .bgsplay file to the configured channel")]
        public async Task ShareAsync()
        {
            if (Context.Message.Attachments.FirstOrDefault(a => a.Filename.EndsWith(".bgsplay", StringComparison.InvariantCultureIgnoreCase)) is not Attachment file)
            {
                return;
            }

            var notifyCommand = new Core.Features.Share.Notify.Command
            {
                Guild = Context.Guild.Id,
                Sender = Context.User.Id,
                Attachment = file
            };

            var notifyTask = _mediator.Send(notifyCommand);

            var photoMessageTask = ResponseUtilities.SkippableWaitForResponse("Send a photo if you'd like to attach one",
                                                                                "Skip",
                                                                                Context.Client,
                                                                                Context.Channel,
                                                                                TimeSpan.FromSeconds(30),
                                                                                Predicate,
                                                                                default);

            var postCommand = new Core.Features.Share.Post.Command
            {
                CommandChannel = Context.Channel.Id,
                Guild = Context.Guild.Id,
                PlayFile = file,
                ImageUrl = (await photoMessageTask)?.Attachments.FirstOrDefault(a => _photoExtensions.Any(e => a.Filename.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))?.Url
            };

            var postTask = _mediator.Send(postCommand);

            await Task.WhenAll(notifyTask, postTask);

            bool Predicate(SocketMessage message) => FromSourceUser(message) && HasPhotoAttachment(message);
            bool FromSourceUser(SocketMessage message) => message.Author.Id == Context.User.Id;
            bool HasPhotoAttachment(SocketMessage message) =>
            message
                .Attachments
                .Select(a => a.Filename.Split(".", StringSplitOptions.RemoveEmptyEntries).Last())
                .Intersect(_photoExtensions)
                .Any();
        }

            //var jsonOptions = new JsonSerializerOptions
            //{
            //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //};

            //jsonOptions.Converters.Add(new DateTimeConverter());
            //jsonOptions.Converters.Add(new IntToBoolConverter());

            //var client = new HttpClient(); //todo - DI
            //var fileContets = await client.GetStreamAsync(file.Url);

            //var playFile = await JsonSerializer.DeserializeAsync<PlayFile>(fileContets, jsonOptions);
            ////todo - null check
            //var play = playFile.MapToPlay();
            //var link = play.CreateShareLink();

            ////var importButton = new ComponentBuilder()
            ////    .WithButton("Import", style: ButtonStyle.Link, url: link)
            ////    .Build();

            //var photoMessage = await photoMessageTask;

            //var embedBuilder = FormatPlay(play);
            //if (photoMessage != null)
            //{
            //    var image = photoMessage.Attachments.First(a => _photoExtensions.Any(e => a.Filename.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)));
            //    embedBuilder.WithImageUrl(image.Url);
            //}



            //await ReplyAsync(embed: embedBuilder.Build());//, components: importButton);
            //}
            //catch (Exception ex)
            //{
            //    ; 
            //    throw;
            //}
            //;

        //private EmbedBuilder FormatPlay(Play play)
        //{
        //    var shareLink = play.CreateShareLink();
        //    ;
        //    return new EmbedBuilder()
        //        .WithTitle(play.Game.Name)
        //        .WithDescription(BuildDescription(play))
        //        .WithFooter(BuildFooter(play))
        //        .WithThumbnailUrl(play.Game.ThumbnailUrl)
        //        .WithTimestamp(play.DatePlayed)
        //        .WithColor(Color.Green)
        //        .WithFields(ScoreFields(play))
        //        .AddField(x =>
        //        {
        //            x.WithName("Import")
        //                .WithValue($"[Click here to import]({shareLink})")
        //                .WithIsInline(true);
        //            ;
        //        })
        //        ;
        //}

        //private IEnumerable<EmbedFieldBuilder> ScoreFields(Play play)
        //{
        //    return play.UsesTeams ? TeamScores(play) : PlayerScores(play);
        //}

        //private IEnumerable<EmbedFieldBuilder> TeamScores(Play play)
        //{
        //    var teams = play.Scores.GroupBy(s => s.Team).OrderBy(t => t.Key);

        //    foreach (var team in teams)
        //    {
        //        var teamName = int.TryParse(team.Key, out var parsed) ? $"Team {++parsed}" : team.Key;
        //        var teamTitle = $"\r\n{teamName}{(team.Any(s => s.Winner) ? " :trophy:" : "")}";
        //        var sb = new StringBuilder();

        //        foreach (var teamScore in team)
        //        {
        //            sb.Append(teamScore.Player.Name);
        //            var calculatedScore = teamScore.CalculateScore();
        //            if (calculatedScore != default)
        //            {
        //                sb.Append($" - {calculatedScore}");
        //            }
        //            sb.AppendLine();
        //            sb.Append("```");
        //            if (!string.IsNullOrEmpty(teamScore.Role)) { sb.AppendLine($"Role: {teamScore.Role}"); }
        //            sb.Append($"BGG: {(string.IsNullOrWhiteSpace(teamScore.Player.BggUsername) ? "Not set" : teamScore.Player.BggUsername)}```");
        //        }

        //        yield return new EmbedFieldBuilder()
        //            .WithName(teamTitle)
        //            .WithValue(sb.ToString())
        //            .WithIsInline(false);
        //    }
        //}

        //private IEnumerable<EmbedFieldBuilder> PlayerScores(Play play)
        //{
        //    var sb = new StringBuilder();

        //    foreach (var score in play.Scores)
        //    {
        //        sb.Append(score.Player.Name);
        //        var calculatedScore = score.CalculateScore();
        //        if (calculatedScore != default)
        //        {
        //            sb.Append($" - {calculatedScore}");
        //        }
        //        if (score.Winner) { sb.Append(" :trophy: "); }
        //        sb.AppendLine();

        //        sb.Append("```");
        //        if (!string.IsNullOrEmpty(score.Role)) { sb.AppendLine($"Role: {score.Role}"); }
        //        sb.Append($"BGG: {(string.IsNullOrEmpty(score.Player.BggUsername) ? "Not set" : score.Player.BggUsername)}```");
        //    }

        //    yield return new EmbedFieldBuilder()
        //        .WithName("Players")
        //        .WithValue(sb.ToString())
        //        .WithIsInline(false)
        //        ;
        //}

        //private string BuildDescription(Play play)
        //{
        //    var descriptionItems = new List<string>
        //    {
        //        play.Location.Name
        //    };

        //    if (play.Rounds != 0)
        //    {
        //        var roundDescription = play.Rounds == 1 ? "1 Round" : $"{play.Rounds} Rounds";
        //        descriptionItems.Add(roundDescription);
        //    }

        //    var hourDescription = play.Duration switch
        //    {
        //        { Hours: 0 } => null,
        //        { Hours: 1 } => "1 hour",
        //        { Hours: var h } => $"{h} hours"
        //    };

        //    var minuteDescription = play.Duration switch
        //    {
        //        { Minutes: 0 } => null,
        //        { Minutes: 1 } => "1 minute",
        //        { Minutes: var m } => $"{m} minutes"
        //    };

        //    var durationDescription = (hourDescription, minuteDescription) switch
        //    {
        //        (not null, not null) => $"{hourDescription} and {minuteDescription}",
        //        (not null, null) => hourDescription,
        //        (null, not null) => minuteDescription,
        //        _ => "Untimed"
        //    };

        //    descriptionItems.Add(durationDescription);

        //    if (play.IgnoredForStatistics)
        //    {
        //        descriptionItems.Add(Format.Bold("Ignored for stats"));
        //    }

        //    var shareLink = play.CreateShareLink();
        //    if (!string.IsNullOrWhiteSpace(shareLink))
        //    {
        //        descriptionItems.Add(Format.Url("Import", shareLink));
        //    }

        //    var sb = new StringBuilder();
        //    return sb.AppendJoin(" - ", descriptionItems).ToString();
        //}

        //private string BuildFooter(Play play)
        //{
        //    var footerItems = new List<string>(3)
        //    {
        //        $"{play.Game.BggYear} {play.Game.Designers}"
        //    };

        //    var style = play switch
        //    {
        //        { Game.Cooperative: true } => "Co-op",
        //        { UsesTeams: true } => "PvP (Teams)",
        //        _ => "PvP"
        //    };

        //    footerItems.Add(style);
        //    footerItems.Add($"{play.Game.MinPlayerCount}-{play.Game.MaxPlayerCount} Players");

        //    var sb = new StringBuilder();
        //    return sb.AppendJoin(" | ", footerItems).ToString();
        //}
    }
}
