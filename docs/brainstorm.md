# Player Linking — Brainstorm

## The Problem

BGStats files identify players by **per-installation UUIDs** — the same real person has a different UUID in each uploader's BGStats database. We need to map these UUIDs to Discord users so embeds can @-mention players, DM results, etc.

The schema is ready (`PlayerMappingEntity` with `(OwnerUserId, BgStatsPlayerUuid) → MappedUserId`), and the parser already extracts `meRefId` (the file owner's player index). Nothing is wired up yet.

Two sub-problems:
1. **Creating mappings** — how do we learn who is who? (the hard UX problem)
2. **Resolving mappings** — how do we apply known mappings on future uploads? (straightforward — legacy code already had this, just needs re-implementation)

---

## Ideas

### 1. meRefId Auto-Link

On upload, use `meRefId` from the file to identify which player is the uploader. Automatically create a `PlayerMapping` for that UUID → the uploading Discord user.

- **Pros**: Zero friction, completely invisible, handles the most common case (people are in their own games), builds up mappings passively over time
- **Cons**: Only links the uploader, not the other 3+ players at the table. Requires the uploader to actually be a player in the game (not always true — spectators, group organizers)

### 2. Embed Buttons (Self-Claim)

After posting a play embed, include a "Claim Player" button or a select menu listing unlinked players. Discord users tap to claim their identity.

- **Pros**: Natural moment (you see the embed and say "that's me"), self-service, no admin needed, can link multiple people per embed
- **Cons**: Clutters every embed with UI, requires Discord component interaction handling (button callbacks, select menus), needs expiry/cleanup logic, only works for people who actually see the embed in the channel

### 3. Message Command (Right-Click)

Add a Discord message command (right-click → Apps → "I'm in this play") on the embed. Shows a select menu of unlinked players.

- **Pros**: Doesn't clutter the embed itself, cleaner than buttons, persistent (works on old embeds too)
- **Cons**: Less discoverable than buttons (most users don't know message commands exist), mobile context-menu UX is clunky, still need component interaction handling

### 4. Slash Command — Admin Maps Players

`/hermod link-player @user bgstats-name:Tyler` — an admin explicitly maps a Discord user to a BGStats player name.

- **Pros**: Explicit, admin-controlled, one mapping covers all future uploads
- **Cons**: Admin needs to know BGStats player names, names aren't unique (UUIDs are the real identifier), high friction, doesn't scale, admin might not know who everyone is

### 5. Slash Command — Self-Registration by Name

`/hermod iam bgstats-name:Tyler` — each user registers their own BGStats display name. Bot matches on future uploads.

- **Pros**: Self-service, one-time setup per user
- **Cons**: Names aren't unique across groups, different uploaders may have different spellings for the same person, name matching is inherently fuzzy and fragile

### 6. Upload-Time Wizard (DM)

After an upload, bot DMs the uploader with a list of players from the file and asks them to @-mention the Discord user for each one.

- **Pros**: Comprehensive — links all players in one shot, works for the first upload
- **Cons**: Very heavy UX, annoying on repeat uploads, DMs may be blocked, pauses the flow, needs "skip" handling for unknowns/guests

### 7. Name Heuristic (Auto-Match)

Try to match BGStats player names to Discord display names or server nicknames in the guild.

- **Pros**: Fully automatic, no user interaction needed
- **Cons**: Unreliable (names rarely match exactly), false positives are worse than no match, fuzzy matching complexity, ambiguous results, feels invasive

### 8. Per-Group Roster (Gradual Learning)

Maintain a roster per group. When a new unrecognized player name appears in an upload, post a message: "New player **Tyler** — who is this? React or reply to claim." Once mapped, never ask again.

- **Pros**: Only asks once per new player, learns over time, group-contextual
- **Cons**: Spammy during onboarding (first upload might have 6 new players), someone needs to respond, what if nobody claims? Prompt messages become noise

### 9. Combo: meRefId + Embed Interaction

Auto-link the uploader via `meRefId` (idea 1). For everyone else, add a lightweight interaction on the embed — either a single "Claim" button or a message command (ideas 2/3).

- **Pros**: Automatic for the most common case, self-service for the rest, low friction overall
- **Cons**: More complex implementation (two mechanisms), still need interaction handling for the second path

### 10. Upload-Time @-Mention Tagging

Uploader includes @-mentions in their message alongside the file: `@Tyler @Jordan @Sam`. Bot matches mentions to players by position or name similarity.

- **Pros**: Piggbacks on existing Discord UX, no bot UI needed
- **Cons**: How to map N mentions to N players? Order-dependent? What if player count doesn't match mention count? Awkward syntax, easy to mess up, doesn't scale

### 11. Invite Link per Player UUID

Bot generates a unique claim URL per unlinked player UUID. Uploader shares the link with each person. Clicking it (authenticated via Discord OAuth) creates the mapping.

- **Pros**: Works for people not in the server, definitive link, no ambiguity
- **Cons**: High friction (share a link for each person?), requires web auth infrastructure (Phase 4), lots of moving parts for a simple mapping

---

## Edge Cases (Any Solution Must Handle)

- **One person, multiple UUIDs**: Same real person has different UUIDs on different uploaders' devices. Mapping is per-owner, so this is inherently handled by the schema — but the user doesn't see it that way
- **Guest/anonymous players**: Some players will never be on Discord. Need a way to leave them unmapped without nagging
- **Player name drift**: BGStats lets you rename players. UUID stays the same but name changes. Mapping by UUID is correct; mapping by name breaks
- **Shared devices**: Rare, but two Discord users might share one BGStats install. meRefId would auto-link to whoever uploads, which might be wrong
- **Cross-group overlap**: Same player appears in uploads from multiple groups. Mappings are per-owner, so they resolve independently — but the user might wonder why they need to be re-linked in a different group
- **Retroactive linking**: When a mapping is created, should old plays be backfilled? Or only future plays?

---

## Initial Instinct

Combo approach (idea 9) seems strongest for v1:
- `meRefId` auto-link covers ~80% of cases with zero effort
- A single embed interaction (button or message command) handles the rest
- Slash command (idea 4 or 5) as an escape hatch for corrections

The wizard (idea 6) and name heuristic (idea 7) feel like over-engineering for the current user base. Roster learning (idea 8) is interesting but spammy. The invite-link approach (idea 11) only makes sense after web auth exists.
