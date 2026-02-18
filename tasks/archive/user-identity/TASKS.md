# Batch: user-identity

## Goal
Replace the `DiscordId` column on `UserEntity` with a dedicated `UserExternalLoginEntity`
table that mirrors the `AspNetUserLogins` shape. This decouples user identity from any single
provider, prepares for ASP.NET Identity migration in Phase 4, and wires a find-or-create
lookup into the play upload flow so uploads can be attributed to a user.

## Task Table

| ID | Task | Status | Depends On |
|----|------|--------|------------|
| 01 | [Add UserExternalLoginEntity](task-01-add-external-login-entity.md) | Completed | — |
| 02 | [Remove UserEntity.DiscordId](task-02-remove-discord-id.md) | Completed | — |
| 03 | [Add EF migration](task-03-ef-migration.md) | Completed | 01, 02 |
| 04 | [Wire sender identity into upload flow](task-04-wire-sender-identity.md) | Completed | 03 |

## Dependency Graph

```
01 ──► 03 ──► 04
02 ──►
```

Tasks 01 and 02 are independent and can be done in parallel. Task 03 (migration) must follow
both. Task 04 can be written in parallel with 03 but requires the migration to run before testing.

## Acceptance Criteria

1. `dotnet build Hermod.slnx` — 0 errors
2. `UserEntity` has no `DiscordId` property
3. `UserExternalLogins` table exists in the SQLite DB after migration
4. Uploading a play file with `?senderDiscordId=<id>` creates or reuses a `Users` row
   and a `UserExternalLogins` row for provider `"Discord"`
5. Re-uploading with the same Discord ID reuses the existing user (no duplicate row)
6. `Plays.UploadedById` is populated when a `senderDiscordId` is supplied
