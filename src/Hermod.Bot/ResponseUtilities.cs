using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Bot
{
    internal static class ResponseUtilities
    {
        private static readonly string _skipId = "skip_custom_id";
        public static async Task<SocketMessage?> SkippableWaitForResponse(
            string message,
            string buttonLabel,
            BaseSocketClient client,
            IMessageChannel channel,
            TimeSpan timeout,
            Predicate<SocketMessage> predicate,
            CancellationToken cancellationToken)
        {
            var component = new ComponentBuilder()
                .WithButton(buttonLabel, customId: _skipId)
                .Build();

            var prompt = await channel.SendMessageAsync(message, components: component);

            var tcs = new TaskCompletionSource<SocketMessage?>();
            var waitCancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task wait = Task
                .Delay(timeout, waitCancelSource.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        tcs.SetResult(null);
                    }
                }, CancellationToken.None);

            cancellationToken.Register(() => tcs.SetCanceled());

            client.MessageReceived += HandleMessage;
            client.InteractionCreated += HandleInteraction;
            var result = await tcs.Task.ConfigureAwait(false);
            client.InteractionCreated -= HandleInteraction;
            client.MessageReceived -= HandleMessage;

            await prompt.DeleteAsync().ConfigureAwait(false);

            return result;

            Task HandleMessage(SocketMessage message)
            {
                if (!message.Author.IsBot && predicate(message))
                {
                    waitCancelSource.Cancel();
                    tcs.SetResult(message);
                }

                return Task.CompletedTask;
            }

            Task HandleInteraction(SocketInteraction interaction)
            {
                if (interaction is SocketMessageComponent component 
                    && component.Message.Id == prompt.Id
                    && component.Data.CustomId == _skipId)
                {
                    waitCancelSource.Cancel();
                    tcs.SetResult(null);
                }

                return Task.CompletedTask;
            }
        }
    }
}
