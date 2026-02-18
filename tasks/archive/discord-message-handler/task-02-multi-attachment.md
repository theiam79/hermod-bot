# Task 02 — Handle Multiple .bgsplay Attachments

## Why
A single Discord message can contain up to 10 attachments. Users might share several play
files at once (e.g. a week's worth of sessions). Each `.bgsplay` attachment is processed
independently but all should be acknowledged with a single ✅ reaction on the original message.

## Steps

This task is a validation and edge-case review of the `MessageReceivedHandler` written in
Task 01. No new files are needed — verify the existing implementation handles these cases:

### 1. Multiple attachments in one message

The `foreach (var attachment in playAttachments)` loop in Task 01 already handles multiple
attachments. Verify the log message includes the filename so it's clear which file contributed
which plays.

### 2. Mixed attachments (some .bgsplay, some not)

The `.Where(a => a.Filename.EndsWith(".bgsplay", ...))` filter should silently skip
non-`.bgsplay` files. Verify that a message with both `.bgsplay` and `.png` attachments
only processes the `.bgsplay` files.

### 3. Attachment download failure

If downloading one attachment fails, the current `try/catch` wraps the entire loop. Consider
whether a single bad attachment should abort all others. If the files are independent,
restructure so each attachment is processed in its own try/catch:

```csharp
foreach (var attachment in playAttachments)
{
    try
    {
        await using var stream = await http.GetStreamAsync(attachment.Url);
        var result = await PlayFileParser.ParseAsync(stream);

        foreach (var play in result.Plays)
            await bus.PublishAsync(new PlayExtracted(play, groupId, senderDiscordId));

        Logger.LogInformation("Dispatched {Count} play(s) from {Filename}",
            result.Plays.Count, attachment.Filename);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to process attachment {Filename}", attachment.Filename);
        // Continue with remaining attachments
    }
}
```

React with ✅ after the loop even if some attachments failed (partial success). Only react
with ❌ if ALL attachments failed.

### 4. Play file with zero plays

`PlayFileParser.ParseAsync` may return a result with an empty `Plays` list (e.g. empty file,
or a file that only has a `meRefId` but no recorded plays). Guard against dispatching nothing:

```csharp
if (result.Plays.Count == 0)
{
    Logger.LogWarning("No plays found in {Filename}", attachment.Filename);
    continue;
}
```

## Notes
- Discord CDN URLs for attachments are time-limited. The download should happen promptly
  in the event handler, not be deferred to later processing.
- The ✅ reaction should appear on the message even if some files had 0 plays — the bot
  still processed them successfully, they just contained no data.

## Acceptance Criteria
- [ ] A message with 3 `.bgsplay` attachments results in plays from all 3 files being persisted
- [ ] A message with mixed attachment types only processes `.bgsplay` files
- [ ] A failed download on one attachment does not prevent processing of others
- [ ] A `.bgsplay` file containing 0 plays logs a warning but does not cause an error
- [ ] ✅ is added after processing even if some individual files had 0 plays
