# Task 04 — Distribution Fan-Out Handler

## Why
After a file is uploaded, we need to identify which players in the file might be linked
to Discord users and dispatch a distribution event for each. This handler fans out
`PlayFileUploaded` into per-player `DistributePlayFile` messages.

## Steps

### 1. Create `src/Hermod.Api/Handlers/DistributeFileHandler.cs`

Wolverine handler for `PlayFileUploaded`. Runs alongside `ExtractPlaysHandler` — both
handle the same message type independently.

```csharp
public static class DistributeFileHandler
{
    public static async Task<OutgoingMessages> Handle(
        PlayFileUploaded message,
        HermodContext db)
    {
        var upload = await db.Uploads
            .FirstOrDefaultAsync(u => u.Id == UploadId.From(message.UploadId));

        if (upload is null) return [];

        var result = PlayFileParser.Parse(Encoding.UTF8.GetString(upload.FileBytes));

        // Collect all unique player UUIDs across all plays
        var allPlayerUuids = result.Plays
            .SelectMany(p => p.Scores)
            .Select(s => s.Player.Uuid.ToString())
            .Distinct()
            .ToList();

        // Exclude the uploader's player UUID
        var meUuidStr = message.MePlayerUuid?.ToString();

        var messages = new OutgoingMessages();
        foreach (var uuid in allPlayerUuids)
        {
            if (uuid == meUuidStr) continue;
            messages.Add(new DistributePlayFile(message.UploadId, uuid));
        }

        return messages;
    }
}
```

## Notes
- Wolverine supports multiple handler classes for the same message type. Both
  `ExtractPlaysHandler` and `DistributeFileHandler` handle `PlayFileUploaded`
  and are discovered automatically.
- Parses the file independently from `ExtractPlaysHandler`. The cost of parsing
  a small JSON file twice is negligible and keeps the handlers fully decoupled.
- Uses `Guid.ToString()` for UUID comparison since `BgStatsPlayerUuid` is stored
  as a string throughout the codebase.
- If a file has no players other than the uploader, no `DistributePlayFile`
  messages are emitted (solo play).

## Acceptance Criteria
- [ ] `DistributeFileHandler` exists and handles `PlayFileUploaded`
- [ ] Emits `DistributePlayFile` for each unique player UUID excluding uploader
- [ ] Solo-player files produce no distribution events
- [ ] `dotnet build Hermod.slnx` succeeds
