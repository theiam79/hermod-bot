# Task Tracker — Upload-First Simplification

## Goal
Rebuild the upload flow using the correct Wolverine event cascade pattern. Bot is colocated in
`Hermod.Api` — no separate process, no cross-process transport, no auth for now.

## Open Tasks

| ID | Task | Status | Depends On |
|----|------|--------|------------|
| 01 | [Remove Hermod.Contracts project](task-01-remove-contracts.md) | Open | — |
| 02 | [Remove auth](task-02-remove-auth.md) | Open | — |
| 03 | [Remove unused endpoints](task-03-remove-unused-endpoints.md) | Open | — |
| 04 | [Remove old handlers and mapper](task-04-remove-old-handlers-mapper.md) | Open | — |
| 05 | [Update PlayEntity — nullable UploadedById, drop RawPlayFileJson](task-05-update-play-entity.md) | Open | — |
| 06 | [Regenerate EF migration](task-06-regenerate-migration.md) | Open | 05 |
| 07 | [Create PlayExtracted and PlayCreated messages](task-07-create-messages.md) | Open | — |
| 08 | [New UploadPlays endpoint](task-08-upload-endpoint.md) | Open | 07 |
| 09 | [PlayExtractedHandler](task-09-play-extracted-handler.md) | Open | 05, 07 |
| 10 | [PlayCreatedHandler stub](task-10-play-created-handler.md) | Open | 07 |

## Dependency Graph

```
01 ──┐
02 ──┤
03 ──┼──► [build passes] ◄── 06 ◄── 05 ──► 09 ◄── 07 ──► 08
04 ──┤                                              └──► 10
     └── (independent cleanup, any order)
```

Tasks 01–05 and 07 are independent and can be done in any order.
Task 06 must follow 05.
Tasks 08, 09, 10 must follow 07 (and 09 must also follow 05).

## Acceptance Criteria (all tasks complete)

1. `dotnet build Hermod.slnx` — clean build, no errors or warnings
2. `~/.aspire/bin/aspire run --project src/Hermod.AppHost` — starts without error
3. `POST /api/plays/upload` with a `.bgsplay` file → 202 Accepted
4. DB contains N `Plays` rows and corresponding `PlayPlayers` rows
5. No `RawPlayFileJson` column in DB schema
6. Logs show "Play {id} created in group {groupId}" from `PlayCreatedHandler`
