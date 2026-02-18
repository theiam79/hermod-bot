# Task 05 — Distribution DM Handler

## Why
The final step: resolve a player UUID to a Discord user and DM them the `.bgsplay` file.
Each `DistributePlayFile` message targets one player UUID. The handler checks whether
that player is linked, whether the user wants DMs, and sends the file.

## Steps

### 1. Create `src/Hermod.Api/Handlers/DistributePlayFileHandler.cs`

```csharp
public static class DistributePlayFileHandler
{
    public static async Task Handle(
        DistributePlayFile message,
        HermodContext db,
        DiscordSocketClient discord,
        ILogger logger)
    {
        // 1. Look up PlayerMappings for this UUID (multiple users allowed)
        var mappings = await db.PlayerMappings
            .Where(pm => pm.BgStatsPlayerUuid == message.PlayerUuid)
            .ToListAsync();

        if (mappings.Count == 0) return;

        // 2. Load the upload to get the file
        var upload = await db.Uploads
            .FirstOrDefaultAsync(u => u.Id == UploadId.From(message.UploadId));

        if (upload is null) return;

        // 3. For each mapped user, check subscription and send DM
        foreach (var mapping in mappings)
        {
            try
            {
                var user = await db.Users
                    .FirstOrDefaultAsync(u => u.Id == mapping.MappedUserId);

                if (user is null || !user.SubscribeToPlays) continue;

                var login = await db.UserExternalLogins
                    .FirstOrDefaultAsync(l =>
                        l.UserId == mapping.MappedUserId && l.Provider == "Discord");

                if (login is null) continue;

                if (!ulong.TryParse(login.ProviderKey, out var discordUserId)) continue;

                var discordUser = await discord.GetUserAsync(discordUserId);
                if (discordUser is null) continue;

                var dmChannel = await discordUser.CreateDMChannelAsync();
                using var stream = new MemoryStream(upload.FileBytes);
                await dmChannel.SendFileAsync(
                    stream,
                    upload.FileName,
                    "Here's a play file you were in — import it into BGStats!");

                logger.LogInformation(
                    "Sent play file {FileName} to Discord user {UserId} for player UUID {Uuid}",
                    upload.FileName, discordUserId, message.PlayerUuid);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to DM play file to user mapped from UUID {Uuid}",
                    message.PlayerUuid);
            }
        }
    }
}
```

## Notes
- **Multiple mappings per UUID**: A single player UUID can map to multiple Discord users.
  Each gets a DM independently.
- **DM failures are swallowed**: Discord DMs can fail if the user has DMs disabled or
  the bot doesn't share a server. Logged as warning, no retry for v1.
- **`SubscribeToPlays` check**: Per-user opt-out via the existing boolean on `UserEntity`
  (defaults to `true`).
- **MemoryStream from byte[]**: The file bytes are already in memory from the DB read.
  `SendFileAsync` takes a `Stream` — wrap in `MemoryStream`.
- **No rate limiting for v1**: File distribution volume is low enough that Discord rate
  limits won't be an issue. If volume grows, consider batching or delays.

## Acceptance Criteria
- [ ] `DistributePlayFileHandler` exists and handles `DistributePlayFile`
- [ ] Linked, subscribed players receive the `.bgsplay` file as a Discord DM
- [ ] Users with `SubscribeToPlays = false` do NOT receive a DM
- [ ] Unlinked player UUIDs are silently skipped
- [ ] DM failures are logged but do not throw or break the pipeline
- [ ] Multiple users linked to the same UUID each receive the file independently
- [ ] `dotnet build Hermod.slnx` succeeds
