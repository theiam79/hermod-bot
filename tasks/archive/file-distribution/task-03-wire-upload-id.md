# Task 03 — Wire UploadId Through Play Pipeline

## Why
`PlayExtracted` now carries `UploadId`. The play creation handler should persist this on
`PlayEntity` so plays link back to their source upload. This enables features like "send
file on claim" — when a user claims a player from an embed, the handler can look up the
source upload and offer the file.

## Steps

### 1. Modify `src/Hermod.Api/Handlers/PlayExtractedHandler.cs`

Set `UploadId` on the `PlayEntity` when creating it:

```csharp
var entity = new PlayEntity
{
    ...existing fields...
    UploadId = message.UploadId.HasValue ? UploadId.From(message.UploadId.Value) : null,
};
```

No other changes needed — the rest of the pipeline (`PlayCreated`, `PostPlay`) doesn't
need `UploadId`.

## Notes
- `UploadId` is nullable on both `PlayExtracted` and `PlayEntity`, so existing plays
  (created before uploads were tracked) are unaffected.
- The FK config from task-01 handles the relationship and `SetNull` on delete.

## Acceptance Criteria
- [ ] `PlayExtractedHandler` sets `PlayEntity.UploadId` from the message
- [ ] Plays created from uploads have a non-null `UploadId` in the database
- [ ] `dotnet build Hermod.slnx` succeeds
