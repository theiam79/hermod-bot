using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Hermod.Api.Messages;
using Hermod.BGStats;
using Hermod.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hermod.Api.Discord;

public class MessageReceivedHandler(
    DiscordSocketClient client,
    ILogger<MessageReceivedHandler> logger,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.MessageReceived += OnMessageReceivedAsync;
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessageReceivedAsync(SocketMessage rawMessage)
    {
        // Ignore bots and system messages
        if (rawMessage.Author.IsBot) return;
        if (rawMessage is not SocketUserMessage message) return;

        // Must be in a guild channel
        if (message.Channel is not SocketGuildChannel guildChannel) return;

        var playAttachments = message.Attachments
            .Where(a => a.Filename.EndsWith(".bgsplay", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (playAttachments.Count == 0) return;

        var guildId = guildChannel.Guild.Id;
        var senderDiscordId = message.Author.Id.ToString();

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<HermodContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // Look up the group for this guild
            var group = await db.Groups.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId);
            var groupId = group?.Id.Value;

            var http = httpClientFactory.CreateClient();
            var processedCount = 0;
            var failureCount = 0;

            foreach (var attachment in playAttachments)
            {
                try
                {
                    await using var stream = await http.GetStreamAsync(attachment.Url);
                    var result = await PlayFileParser.ParseAsync(stream);

                    if (result.Plays.Count == 0)
                    {
                        logger.LogWarning("No plays found in {Filename} uploaded by {Author}", 
                            attachment.Filename, message.Author.Username);
                        processedCount++;
                        continue;
                    }

                    foreach (var play in result.Plays)
                    {
                        await bus.PublishAsync(new PlayExtracted(play, groupId, senderDiscordId, result.MePlayerUuid));
                    }

                    logger.LogInformation(
                        "Dispatched {Count} play(s) from {Filename} uploaded by {Author}",
                        result.Plays.Count, attachment.Filename, message.Author.Username);
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process attachment {Filename} from {Author}", 
                        attachment.Filename, message.Author.Username);
                    failureCount++;
                }
            }

            if (processedCount > 0)
            {
                await message.AddReactionAsync(new Emoji("✅"));
            }
            
            if (failureCount > 0 && processedCount == 0)
            {
                await message.AddReactionAsync(new Emoji("❌"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process .bgsplay attachments from {Author} in {Guild}",
                message.Author.Username, guildChannel.Guild.Name);
            await message.AddReactionAsync(new Emoji("❌"));
        }
    }
}
