# Batch: group-setup

## Goal
Give server admins slash commands to configure how Hermod behaves in their guild. This covers
three settings: opting the server into play sharing, designating the channel where embeds are
posted, and tuning the multi-play threshold. All commands require the `Manage Guild` Discord
permission.

## Task Table

| ID | Task | Status | Depends On |
|----|------|--------|------------|
| 01 | [Add SpamThreshold to GroupEntity](task-01-spam-threshold-entity.md) | Open | — |
| 02 | [GroupAdmin slash commands](task-02-group-admin-commands.md) | Open | 01, discord-bot-foundation |

## Dependency Graph

```
01 ──► 02
```

## Acceptance Criteria

1. `dotnet build Hermod.slnx` — 0 errors
2. `/hermod allow-sharing true` sets `Groups.AllowSharing = true` for the calling guild
3. `/hermod set-channel #channel` sets `Groups.DiscordPostChannelId`
4. `/hermod set-threshold 5` sets `Groups.SpamThreshold = 5`
5. All three commands respond ephemerally (only the invoker sees the confirmation)
6. All three commands fail gracefully if the guild has no `Groups` row
7. Users without `Manage Guild` permission cannot invoke the commands
