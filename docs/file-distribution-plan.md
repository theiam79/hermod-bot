# File Distribution — Implementation Plan

_DM linked players their .bgsplay file for import into BGStats_

## Overview

When a user uploads a `.bgsplay` file, linked players should receive the file
via Discord DM so they can import the play into their own BGStats app. This is
a file-forwarding feature, not a notification — the actual `.bgsplay` file is
the payload.

**Key design decisions:**

- **One DM per upload** — not per play. A single `.bgsplay` file may contain
  multiple plays; the recipient gets the whole file once.
- **Parallel to embed posting** — distribution fires from the same trigger as
  play extraction, not as a continuation of the embed pipeline.
- **Uploader excluded** — they already have the file.
- **Unlinked players silently skipped** — no mapping means no DM. If they link
  later, they don't retroactively receive old files (not mandatory for v1).
- **Per-user opt-out** — `UserEntity.SubscribeToPlays` (existing, defaults true)
  controls whether a linked user receives DMs.

---

## Pipeline Refactor

### Current flow

```
MessageReceivedHandler
  - Downloads attachment
  - Parses file (PlayFileParser)
  - Dispatches PlayExtracted per play
       │
       ▼
PlayExtractedHandler → PlayCreated → PostPlay → embed
```

### New flow

```
MessageReceivedHandler / UploadPlays
  - Downloads attachment bytes
  - Stores UploadEntity (file bytes, uploader, filename)
  - Dispatches PlayFileUploaded(uploadId, groupId, senderDiscordId, mePlayerUuid)
       │
       ▼
PlayFileUploaded
  ├─► ExtractPlaysHandler
  │     - Reads file bytes from UploadEntity
  │     - Parses with PlayFileParser
  │     - Emits PlayExtracted per play (now carries UploadId)
  │         └─► existing pipeline: PlayExtractedHandler → PlayCreated → PostPlay → embed
  │
  └─► DistributeFileHandler
        - Reads file bytes from UploadEntity
        - Parses to get unique player UUIDs
        - Excludes MePlayerUuid (uploader)
        - Emits DistributePlayFile(uploadId, playerUuid) per remaining UUID
            │
            ▼
        DistributePlayFileHandler
          - Looks up PlayerMapping for UUID → MappedUserId
          - If no mapping → no-op
          - Checks UserEntity.SubscribeToPlays → if false, skip
          - Resolves Discord ID from UserExternalLogins
          - DMs the .bgsplay file as an attachment
```

Both handlers for `PlayFileUploaded` are independent. Wolverine discovers and
chains them both. They each parse the file (small JSON, negligible cost) and
cascade different message types.

---

## Schema Changes

### New: UploadEntity

```
UploadEntity
  Id: UploadId              (Vogen, Guid)
  UploadedById: UserId?     (FK → UserEntity, nullable for API uploads without auth)
  FileBytes: byte[]         (raw .bgsplay content)
  FileName: string          (original filename)
  CreatedAt: DateTime
```

DB blob storage is sufficient — files are small and volume is low.

### Modified: PlayEntity

```
PlayEntity
  ...existing fields...
  UploadId: UploadId?       (FK → UploadEntity, nullable for existing plays)
```

Links plays back to their source file. Enables the claim flow to offer the file
immediately when a user links from an embed.

### New Vogen type

Add `UploadId` to `Ids.cs`.

---

## Messages

### New: PlayFileUploaded

```csharp
public record PlayFileUploaded(Guid UploadId, Guid? GroupId, string? SenderDiscordId, Guid? MePlayerUuid);
```

Dispatched once per file upload. Triggers both play extraction and file distribution.

### New: DistributePlayFile

```csharp
public record DistributePlayFile(Guid UploadId, string PlayerUuid);
```

Dispatched once per unique player UUID (minus uploader). The handler resolves
whether the player is linked and subscribed.

### Modified: PlayExtracted

```csharp
public record PlayExtracted(Play ParsedPlay, Guid? GroupId, string? SenderDiscordId, Guid? MePlayerUuid, Guid? UploadId);
```

Added `UploadId` so `PlayExtractedHandler` can set `PlayEntity.UploadId`.

---

## Handler Details

### ExtractPlaysHandler

Handles `PlayFileUploaded`. Reads the `UploadEntity` from DB, parses the file,
and emits `PlayExtracted` per play. This is the logic currently in
`MessageReceivedHandler` — it moves into a proper Wolverine handler.

Returns `OutgoingMessages` containing all `PlayExtracted` messages.

### DistributeFileHandler

Handles `PlayFileUploaded`. Reads the `UploadEntity` from DB, parses the file
to collect all unique player UUIDs across all plays. Excludes `MePlayerUuid`
(the uploader). Emits `DistributePlayFile(uploadId, playerUuid)` per remaining
UUID.

Returns `OutgoingMessages` containing all `DistributePlayFile` messages.

### DistributePlayFileHandler

Handles `DistributePlayFile`. Single-responsibility: resolve one player UUID
to a Discord DM.

1. Query `PlayerMappings` where `BgStatsPlayerUuid == playerUuid`
2. If no mapping → return (no-op)
3. For each mapped user (multiple allowed):
   a. Load `UserEntity` → check `SubscribeToPlays`
   b. If false → skip
   c. Query `UserExternalLogins` for Discord provider → get Discord ID
   d. Get Discord user → create DM channel → send file as attachment
4. Brief message text: e.g. "Here's a play file you were in — import it into BGStats!"

### Modified: MessageReceivedHandler

No longer parses or dispatches `PlayExtracted`. New flow:

1. Download attachment bytes
2. Create scope, get db + bus
3. `FindOrCreateUserAsync` for the sender (moved here from PlayExtractedHandler? No — keep in PlayExtractedHandler since it's per-play)
4. Store `UploadEntity` with file bytes, filename, uploader reference
5. `await db.SaveChangesAsync()` — must commit before Wolverine handler reads it
6. `await bus.PublishAsync(new PlayFileUploaded(...))`

### Modified: UploadPlays

Same refactor: read file bytes, store `UploadEntity`, emit `PlayFileUploaded`.
Since this is a Wolverine HTTP endpoint, the `UploadEntity` save is part of the
unit of work and commits before the cascaded message is processed.

### Modified: PlayExtractedHandler

Set `PlayEntity.UploadId` from `PlayExtracted.UploadId`.

---

## Resolved Decisions

1. **File storage**: DB blob on `UploadEntity`. Files are small JSON, volume is
   low, and it avoids introducing external storage.
2. **Parse twice**: Both handlers for `PlayFileUploaded` parse the file
   independently. The cost is negligible for small JSON and keeps the handlers
   fully decoupled.
3. **Uploader exclusion**: By `MePlayerUuid`, not by checking all of the
   uploader's linked UUIDs. Simple and correct — the uploader IS the "me" player.
4. **No retroactive distribution**: If a player links after the file was uploaded,
   they don't receive old files. Could be added later but not required for v1.
5. **DM failures**: Log and swallow. Discord DMs can fail if the user has DMs
   disabled or doesn't share a server. No retry mechanism for v1.
6. **UploadId on PlayEntity**: Enables future "send file on claim" — when a user
   claims a player via the embed, the handler can look up the source upload and
   offer the file. Not implemented in this batch but the schema supports it.
