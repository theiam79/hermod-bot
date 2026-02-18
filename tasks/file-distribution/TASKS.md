# Batch: file-distribution

## Goal
DM linked players their `.bgsplay` file after upload so they can import the play into their
own BGStats app. Introduces an `UploadEntity` to persist raw file bytes, refactors the upload
pipeline to fan out through a `PlayFileUploaded` event, and adds a distribution handler that
DMs the file to linked, subscribed players.

Full design: `docs/file-distribution-plan.md`

## Task Table

| ID | Task | Status | Depends On |
|----|------|--------|------------|
| 01 | [UploadEntity schema + messages](task-01-schema-and-messages.md) | Open | — |
| 02 | [Refactor upload pipeline](task-02-refactor-upload-pipeline.md) | Open | 01 |
| 03 | [Wire UploadId through play pipeline](task-03-wire-upload-id.md) | Open | 02 |
| 04 | [Distribution fan-out handler](task-04-distribution-fanout.md) | Open | 01 |
| 05 | [Distribution DM handler](task-05-distribution-dm.md) | Open | 04 |

## Dependency Graph

```
01 ──┬──► 02 ──► 03
     │
     └──► 04 ──► 05
```

02–03 and 04–05 are parallel branches after 01.

## Acceptance Criteria

1. `dotnet build Hermod.slnx` — 0 errors
2. Uploading a `.bgsplay` file stores the raw bytes in `UploadEntity`
3. Plays are still extracted and posted as embeds (existing behavior preserved)
4. `PlayEntity.UploadId` links each play back to its source upload
5. Linked players (via `PlayerMappings`) receive the `.bgsplay` file as a Discord DM
6. The uploader does NOT receive a DM
7. Users with `SubscribeToPlays = false` do NOT receive a DM
8. Unlinked players are silently skipped (no error)
9. DM failures are logged but do not break the pipeline
