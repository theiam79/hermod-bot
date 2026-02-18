# Task 02 — Refactor Upload Pipeline

## Why
Move file parsing out of the Discord hosted service and HTTP endpoint into Wolverine handlers.
The upload entry points become thin: store the raw file, emit `PlayFileUploaded`. A new
`ExtractPlaysHandler` takes over the parsing and `PlayExtracted` dispatch.

## Steps

### 1. Refactor `src/Hermod.Api/Discord/MessageReceivedHandler.cs`

Replace the per-attachment parse-and-dispatch loop with:

1. Download attachment bytes into a `byte[]` (via `HttpClient.GetByteArrayAsync`)
2. Store an `UploadEntity`:
   ```csharp
   var upload = new UploadEntity
   {
       Id = UploadId.From(Guid.NewGuid()),
       FileBytes = fileBytes,
       FileName = attachment.Filename,
       CreatedAt = DateTime.UtcNow,
   };
   db.Uploads.Add(upload);
   ```
3. Parse the file (from bytes) just enough to extract `MePlayerUuid`:
   ```csharp
   var result = PlayFileParser.Parse(Encoding.UTF8.GetString(fileBytes));
   ```
   We parse here only to get `MePlayerUuid` for the message. The full play
   extraction happens in the handler.
4. `await db.SaveChangesAsync()` — commit before publishing so the handler can
   read the upload
5. Emit `PlayFileUploaded`:
   ```csharp
   await bus.PublishAsync(new PlayFileUploaded(
       upload.Id.Value, groupId, senderDiscordId, result.MePlayerUuid));
   ```

Remove the per-play `PlayExtracted` dispatch loop entirely.

The reaction logic (checkmark/cross) stays — react after all attachments are stored
and events dispatched.

### 2. Refactor `src/Hermod.Api/Endpoints/Plays/UploadPlays.cs`

Same pattern: read file bytes, store `UploadEntity`, emit `PlayFileUploaded`.

Since this is a Wolverine HTTP endpoint with `OutgoingMessages`, the upload entity
is saved as part of the unit of work. The cascaded message is processed after commit.

```csharp
[WolverinePost("/api/plays/upload")]
public static async Task<(IResult, OutgoingMessages)> Post(
    IFormFile file,
    [FromQuery] Guid? groupId,
    [FromQuery] string? senderDiscordId,
    HermodContext db)
{
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var fileBytes = ms.ToArray();

    var upload = new UploadEntity
    {
        Id = UploadId.From(Guid.NewGuid()),
        FileBytes = fileBytes,
        FileName = file.FileName,
        CreatedAt = DateTime.UtcNow,
    };
    db.Uploads.Add(upload);

    var result = PlayFileParser.Parse(Encoding.UTF8.GetString(fileBytes));

    var messages = new OutgoingMessages();
    messages.Add(new PlayFileUploaded(
        upload.Id.Value, groupId, senderDiscordId, result.MePlayerUuid));

    return (Results.Accepted(), messages);
}
```

### 3. Create `src/Hermod.Api/Handlers/ExtractPlaysHandler.cs`

New Wolverine handler that takes over the play extraction logic:

```csharp
public static class ExtractPlaysHandler
{
    public static async Task<OutgoingMessages> Handle(
        PlayFileUploaded message,
        HermodContext db)
    {
        var upload = await db.Uploads
            .FirstOrDefaultAsync(u => u.Id == UploadId.From(message.UploadId));

        if (upload is null) return [];

        var result = PlayFileParser.Parse(Encoding.UTF8.GetString(upload.FileBytes));

        var messages = new OutgoingMessages();
        foreach (var play in result.Plays)
        {
            messages.Add(new PlayExtracted(
                play, message.GroupId, message.SenderDiscordId,
                message.MePlayerUuid, message.UploadId));
        }

        return messages;
    }
}
```

## Notes
- `MessageReceivedHandler` still needs to parse once to get `MePlayerUuid`. This is
  unavoidable without storing it separately. The cost is negligible.
- The handler parses again from the stored bytes. Two parses per upload is acceptable.
- `PlayFileParser.Parse(string)` already exists and works with the JSON string.
- `MessageReceivedHandler` calls `SaveChangesAsync` explicitly because it's outside
  Wolverine's unit-of-work (it's a Discord hosted service creating its own scope).
- `UploadPlays` does NOT call `SaveChangesAsync` — Wolverine's auto-transaction handles it.

## Acceptance Criteria
- [ ] `MessageReceivedHandler` stores `UploadEntity` and emits `PlayFileUploaded`
- [ ] `MessageReceivedHandler` no longer dispatches `PlayExtracted` directly
- [ ] `UploadPlays` stores `UploadEntity` and emits `PlayFileUploaded`
- [ ] `ExtractPlaysHandler` exists and emits `PlayExtracted` per parsed play
- [ ] Uploading a `.bgsplay` file still results in plays being created and embeds posted
- [ ] `dotnet build Hermod.slnx` succeeds
