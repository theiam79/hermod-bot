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

namespace Hermod.Bot.Modules.Share
{
    internal class Module : ModuleBase<SocketCommandContext>
    {
        private readonly IMediator _mediator;
        private readonly List<string> _photoExtensions;
        private readonly IServiceProvider _serviceProvider;

        public Module(IMediator mediator, IServiceProvider serviceProvider)
        {
            _mediator = mediator;

            _photoExtensions = new List<string>()
            {
                "jpg",
                "jpeg",
                "png"
            };
            _serviceProvider = serviceProvider;
        }

        [Command("share")]
        [Summary("Shares a .bgsplay file to the configured channel")]
        public async Task ShareAsync()

        {
            if (Context.Message.Attachments.FirstOrDefault(a => a.Filename.EndsWith(".bgsplay", StringComparison.InvariantCultureIgnoreCase)) is not Attachment file)
            {
                await ReplyAsync("You must attach a valid .bgsplay file");
                return;
            }

            var notifyCommand = new Core.Features.Share.Notify.Command
            {
                Guild = Context.Guild?.Id ?? 1,
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
                Sender = Context.User.Id,
                CommandChannel = Context.Channel.Id,
                Guild = Context.Guild?.Id,
                PlayFile = file,
                ImageUrl = (await photoMessageTask)?.Attachments.FirstOrDefault(a => _photoExtensions.Any(e => a.Filename.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))?.Url
            };

            var postTask = _mediator.Send(postCommand);


            var finalResult = Result.Merge(await Task.WhenAll(notifyTask, postTask));

            await ReplyAsync(embed: finalResult.ToEmbed());
            

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
