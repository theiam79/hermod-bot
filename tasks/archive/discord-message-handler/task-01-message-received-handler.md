# Task 01 — MessageReceivedHandler Service

## Why
The primary Discord upload path is a user dropping a `.bgsplay` file into a channel. The
`MessageReceivedHandler` service watches for these messages and feeds them into the same
Wolverine pipeline as the HTTP upload endpoint. This means all play persistence and
distribution logic is shared — the bot is just another input source.

## Steps

### Create `src/Hermod.Api/Discord/MessageReceivedHandler.cs`

```csharp
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

            foreach (var attachment in playAttachments)
            {
                await using var stream = await http.GetStreamAsync(attachment.Url);
                var result = await PlayFileParser.ParseAsync(stream);

                foreach (var play in result.Plays)
                    await bus.PublishAsync(new PlayExtracted(play, groupId, senderDiscordId));

                Logger.LogInformation(
                    "Dispatched {Count} play(s) from {Filename} uploaded by {Author}",
                    result.Plays.Count, attachment.Filename, message.Author.Username);
            }

            await message.AddReactionAsync(new Emoji("✅"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process .bgsplay attachment from {Author} in {Guild}",
                message.Author.Username, guildChannel.Guild.Name);
            await message.AddReactionAsync(new Emoji("❌"));
        }
    }
}
```

### Register in `src/Hermod.Api/Program.cs`

After the existing `AddHostedService` calls:

```csharp
builder.Services.AddHostedService<MessageReceivedHandler>();
```

Also ensure `IHttpClientFactory` is registered. `AddHttpClient()` is typically already called
via `AddServiceDefaults()` from Aspire's service defaults. If not, add:

```csharp
builder.Services.AddHttpClient();
```

## Notes
- `IServiceScopeFactory` is required because `HermodContext` and `IMessageBus` are scoped
  services and `MessageReceivedHandler` is a singleton.
- `IHttpClientFactory` is used (not `new HttpClient()`) to avoid socket exhaustion on
  repeated attachment downloads.
- The ✅ reaction is added after all attachments in the message are dispatched. If any fail,
  ❌ is added instead and the error is logged.
- `groupId` may be `null` if the guild doesn't have a `Groups` row yet (shouldn't happen
  after GuildHandler runs, but defensive null-handling is correct here).
- Only the first `.bgsplay` attachment per message is guaranteed to work; Task 02 covers the
  multi-attachment case in detail. This task handles it generically with a foreach.

## Acceptance Criteria
- [ ] `src/Hermod.Api/Discord/MessageReceivedHandler.cs` exists
- [ ] `dotnet build src/Hermod.Api/Hermod.Api.csproj` succeeds
- [ ] Dropping a `.bgsplay` in a guild channel triggers processing and ✅ reaction
- [ ] Bot messages and non-`.bgsplay` attachments are ignored
- [ ] Plays are persisted in the database after the reaction appears
