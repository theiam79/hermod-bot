# Batch: play-embed-posting

## Goal
When a play is created in a group that has opted into sharing, post a Discord embed to the
configured channel. Track each posted message in a `PlayPosts` table so embeds can be edited
or deleted later. Individual embeds are posted per play (threshold/summary logic is a
follow-on batch once the UX is validated).

## Task Table

| ID | Task | Status | Depends On |
|----|------|--------|------------|
| 01 | [PlayPostEntity and migration](task-01-play-post-entity.md) | Open | — |
| 02 | [PostPlay message and PlayCreatedHandler update](task-02-post-play-message.md) | Open | 01 |
| 03 | [PostPlayHandler — build and send embed](task-03-post-play-handler.md) | Open | 02, group-setup, discord-bot-foundation |

## Dependency Graph

```
01 ──► 02 ──► 03
```

## Acceptance Criteria

1. `dotnet build Hermod.slnx` — 0 errors
2. Uploading a play in a group with `AllowSharing = true` and a configured post channel
   results in an embed appearing in that channel
3. The `PlayPosts` table contains one row per posted embed, with correct `DiscordMessageId`
4. Uploading in a group with `AllowSharing = false` does NOT post an embed
5. Uploading with no `GroupId` does NOT post an embed
6. The embed includes: game name, date played, each player with score/winner indicator
7. Uploading in a group with no `DiscordPostChannelId` configured does NOT post an embed
   (and does not error)
