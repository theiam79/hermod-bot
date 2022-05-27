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
using FluentResults;
using Hermod.Bot.Extensions;

namespace Hermod.Bot.Modules
{
    internal class Share : ModuleBase<SocketCommandContext>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Share> _logger;

        private readonly List<string> _photoExtensions;

        public Share(IMediator mediator, ILogger<Share> logger)
        {
            _mediator = mediator;

            _photoExtensions = new List<string>()
            {
                "jpg",
                "jpeg",
                "png"
            };
            _logger = logger;
        }

        [Command("share")]
        [Summary("Shares a .bgsplay file to the configured channel")]
        public async Task ShareAsync()
        {
            var playFiles = Context.Message.Attachments.Where(a => a.Filename.EndsWith(".bgsplay", StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (playFiles.Count <= 0)
            {
                await ReplyAsync("You must attach a valid .bgsplay file");
                return;
            }

            try
            {
                string? imageUrl = null;
                if (playFiles.Count == 1)
                {
                    _logger.LogDebug("Waiting for photo or skip button press");
                    var photoMessageTask = ResponseUtilities.SkippableWaitForResponse("Send a photo if you'd like to attach one",
                                                                                       "Skip",
                                                                                       Context.Client,
                                                                                       Context.Channel,
                                                                                       TimeSpan.FromSeconds(30),
                                                                                       Predicate,
                                                                                       default);

                    imageUrl = (await photoMessageTask)?.Attachments.FirstOrDefault(a => _photoExtensions.Any(e => a.Filename.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))?.Url;
                    _logger.LogDebug("Photo received or skip button pressed");
                }

                List<Task<Result>> tasks = new();

                foreach (var play in playFiles)
                {
                    _logger.LogDebug("Sending notify command");
                    var notifyCommand = new Core.Features.Share.Notify.Command(Context.User.Id, play);
                    tasks.Add(_mediator.Send(notifyCommand));

                    _logger.LogDebug("Sending post command");
                    var postCommand = new Core.Features.Share.Post.Command(Context.User.Id, play, imageUrl);
                    tasks.Add(_mediator.Send(postCommand));
                }




                _logger.LogDebug("Waiting for post and notify commands to complete");
                var finalResult = Result.Merge(await Task.WhenAll(tasks));
                await ReplyAsync(embed: finalResult.ToEmbed());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong when attempting to share {PlayFiles}", playFiles);
            }

            bool Predicate(SocketMessage message) => FromSourceUser(message) && HasPhotoAttachment(message);
            bool FromSourceUser(SocketMessage message) => message.Author.Id == Context.User.Id;
            bool HasPhotoAttachment(SocketMessage message) =>
            message
                .Attachments
                .Select(a => a.Filename.Split(".", StringSplitOptions.RemoveEmptyEntries).Last())
                .Intersect(_photoExtensions)
                .Any();
        }
    }
}
