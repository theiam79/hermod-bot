# Player Linking — Implementation Plan

_meRefId auto-link + message command self-claim, global UUID mapping_

## Overview

Two linking mechanisms, layered:

1. **meRefId auto-link** — on every upload, automatically create a `PlayerMapping` for the uploading user based on the `meRefId` field in the `.bgsplay` file. Then resolve all known mappings on every play in the file. Zero user interaction needed.

2. **Message command self-claim** — a Discord message command ("Claim Player") on play embeds. Any user right-clicks the embed, selects their player from a list, and a mapping is created. Works for everyone who wasn't the uploader.

**Key design decisions:**

- **Global UUID mapping** — mappings are NOT scoped per-uploader. A single `PlayerMapping(BgStatsPlayerUuid) → MappedUserId` covers all uploads containing that UUID. UUID collisions between different real people are astronomically unlikely, and the common shared-UUID case (backup/player imports) represents the same person anyway.
- **Multiple users per UUID allowed** — more than one Discord user can link to the same UUID. Primary use case is file distribution (future). If a link is wrong, users can unlink themselves.
- **Embeds always show the BGStats player name** — no @-mentions in embeds. Keeps the display simple and avoids ambiguity when multiple users are linked to one UUID. Linking is purely for backend data enrichment and future features (DMs, stats, profiles).

---

## Schema Change

### Current schema
```
PlayerMappingEntity
  Id: PlayerMappingId
  OwnerUserId: UserId          ← REMOVE
  BgStatsPlayerUuid: string
  MappedUserId: UserId
  Unique index: (OwnerUserId, BgStatsPlayerUuid)  ← CHANGE
```

### New schema
```
PlayerMappingEntity
  Id: PlayerMappingId
  BgStatsPlayerUuid: string
  MappedUserId: UserId
  Unique index: (BgStatsPlayerUuid, MappedUserId)  ← one user can't link same UUID twice
```

Remove `OwnerUserId`, `Owner` nav property, and the cascade FK to `UserEntity` via `Owner`. Keep the `MappedUser` FK. Update `UserEntity.PlayerMappings` nav to go through `MappedUserId` instead of `OwnerUserId`. Generate a migration.

---

## Part A: meRefId Auto-Link + Mapping Resolution

### A1. Resolve meRefId to UUID in the parser

`PlayFileResult` already has `MeRefId` (int). Add `MePlayerUuid` (Guid?) so downstream consumers don't need access to the raw player list.

```
PlayFileResult
  MeRefId: int           (existing)
  MePlayerUuid: Guid?    (new — resolved from Players.FirstOrDefault(p => p.Id == MeRefId)?.Uuid)
  Plays: List<Play>      (existing)
```

Returns null if `MeRefId` doesn't match any player (handles malformed files, `MeRefId: 0`, or exports from users who don't identify as a player).

### A2. Thread me-player UUID through the message pipeline

`MessageReceivedHandler` has access to `PlayFileResult`. Pass the resolved UUID into each `PlayExtracted` message:

```
PlayExtracted
  ParsedPlay: Play           (existing)
  GroupId: Guid?              (existing)
  SenderDiscordId: string?    (existing)
  MePlayerUuid: string?       (new)
```

### A3. Auto-link the uploader in PlayExtractedHandler

After the existing `FindOrCreateUserAsync` call, if `MePlayerUuid` is set and an uploader was identified:

1. Check if `PlayerMapping(mePlayerUuid, uploaderId)` already exists
2. If not, create it — the uploader IS the "me" player

Idempotent — the unique index on `(BgStatsPlayerUuid, MappedUserId)` prevents duplicates.

### A4. Resolve existing mappings for all players in the play

Still in `PlayExtractedHandler`, after creating the `PlayEntity` and its `PlayPlayerEntity` rows:

1. Collect all `BgStatsPlayerUuid` values from the play's players
2. Query `PlayerMappings` where UUID is in the set
3. For each match, set `PlayPlayerEntity.MappedUserId`

Since multiple users can map to the same UUID, pick any (or the first). The `MappedUserId` on `PlayPlayerEntity` is singular — it represents "a known Discord user who is this player." For file distribution later, the query would go directly to `PlayerMappings` by UUID to find ALL linked users, not through `PlayPlayerEntity.MappedUserId`.

This happens within the same Wolverine unit of work — no extra `SaveChangesAsync` needed.

---

## Part B: Message Command Self-Claim

### B1. Register a message command

Create a Discord message command module. Message commands appear when a user right-clicks a message → Apps.

```
[MessageCommand("Claim Player")]
```

The handler receives the target message (the embed) as context.

### B2. Look up the play from the embed message

Use the `PlayPosts` table to find the play:

```
PlayPosts.FirstOrDefault(pp => pp.DiscordMessageId == targetMessage.Id)
  → PlayId → load PlayEntity with Players included
```

If no `PlayPost` row exists (message isn't a Hermod embed), respond with an ephemeral error.

### B3. Show a select menu of players

Build a `SelectMenuBuilder` listing all players from the play. Each option:
- Label: player name
- Value: `BgStatsPlayerUuid`
- Description: score if available (for disambiguation when names aren't unique)

Show all players regardless of link status — the user knows who they are.

Respond with an ephemeral message containing the select menu.

### B4. Handle the select menu interaction

When the user picks a player:

1. Find-or-create a `UserEntity` + `UserExternalLoginEntity` for the claiming user (same `FindOrCreateUserAsync` pattern)
2. Check if `PlayerMapping(selectedUuid, claimingUserId)` already exists
3. If not, create it
4. Set `PlayPlayerEntity.MappedUserId` on the selected player row for this play (if not already set)
5. Respond: "You've been linked as **PlayerName**."

### B5. Backfill historical plays

After creating the mapping in B4, update all `PlayPlayerEntity` rows where:
- `BgStatsPlayerUuid` matches the claimed UUID
- `MappedUserId` is null

This makes the claim retroactive across all plays globally, not just the one the user clicked on. Since the UUID is globally unique to a real person, this is safe.

---

## Part C: Unlink

### C1. Slash command to view and remove links

`/hermod my-players` — shows the claiming user's current player links as an ephemeral list:
- Each entry: BGStats player name (from the most recent play containing that UUID) + UUID
- A select menu or buttons to unlink individual entries

### C2. Handle unlink

When a user unlinks:
1. Delete the `PlayerMapping` row
2. Optionally clear `MappedUserId` on `PlayPlayerEntity` rows where it points to this user AND the UUID matches (or leave them — they're historical records)

---

## Data Flow (After Implementation)

```
.bgsplay file dropped
       │
       ▼
MessageReceivedHandler
  - Parses file → PlayFileResult (MePlayerUuid resolved)
  - Dispatches PlayExtracted per play (carries MePlayerUuid)
       │
       ▼
PlayExtractedHandler
  - Find-or-create user from Discord ID
  - Auto-link uploader via MePlayerUuid → PlayerMapping     ←── A3
  - Create PlayEntity + PlayPlayerEntities
  - Resolve known PlayerMappings → set MappedUserId          ←── A4
  - Return PlayCreated
       │
       ▼
PlayCreatedHandler → PostPlay? → PostPlayHandler
  - Build embed (player names from file, no @-mentions)
  - Post to Discord channel
  - Record PlayPost

                    ┌──────────────────────────────────────┐
                    │  User right-clicks embed             │
                    │  → Apps → "Claim Player"       B1-B2 │
                    │  → Select menu of players        B3  │
                    │  → Pick player                       │
                    │  → Create PlayerMapping           B4  │
                    │  → Backfill older plays           B5  │
                    └──────────────────────────────────────┘

                    ┌──────────────────────────────────────┐
                    │  /hermod my-players               C1  │
                    │  → View linked UUIDs                 │
                    │  → Unlink                         C2  │
                    └──────────────────────────────────────┘
```

---

## Resolved Decisions

1. **Backfill on claim**: only set `MappedUserId` where null — never overwrite an existing link. To fix a wrong link: unlink first, then re-claim.
2. **Keep `PlayPlayerEntity.MappedUserId`**: useful denormalized snapshot for quick "is this player linked?" checks. Authoritative multi-user source remains `PlayerMappings` by UUID (used for file distribution later).
3. **Guest players**: do nothing. Unclaimed is the natural default. No explicit marker needed unless a future feature requires distinguishing "nobody tried" from "not on Discord."
4. **meRefId 0 or invalid**: skip auto-link silently. `MePlayerUuid` resolves to null, no error, no warning — normal case for users who don't mark themselves as a player.
5. **Select menu >25 players**: truncate with a note. Party games with 25+ individual player entries in BGStats are vanishingly rare.
