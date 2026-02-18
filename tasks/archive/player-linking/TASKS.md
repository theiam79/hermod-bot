# Batch: player-linking (Archived)

## Goal
Map Discord users to BGStats player UUIDs so future features (DMs, stats, profiles) can
identify who played. Three mechanisms: meRefId auto-link on upload, message command
self-claim on embeds, and `/hermod my-players` for viewing/unlinking.

Full design: `docs/player-linking-plan.md`

## Task Table

| ID | Task | Status |
|----|------|--------|
| 01 | Schema migration — simplify PlayerMappingEntity to global mapping | Done |
| 02 | Thread MePlayerUuid from parser through pipeline | Done |
| 03 | Auto-link uploader + resolve mappings in PlayExtractedHandler | Done |
| 04 | Claim Player message command with select menu + backfill | Done |
| 05 | /hermod my-players + unlink command | Done |

## Commits
- `7759547` — Implement player-linking batch
