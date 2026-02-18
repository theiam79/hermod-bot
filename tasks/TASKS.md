# Task Tracker

## Active Batches

| Batch | Description | Status |
|-------|-------------|--------|
| [discord-bot-foundation](discord-bot-foundation/) | Wire Discord.Net into Hermod.Api as hosted services; connect bot, register slash commands, sync guilds | Planned |
| [user-identity](user-identity/) | Replace UserEntity.DiscordId with UserExternalLoginEntity table; wire find-or-create into upload flow | Planned |
| [discord-message-handler](discord-message-handler/) | Process .bgsplay attachments dropped in Discord channels; react ✅ and dispatch through Wolverine pipeline | Planned |
| [group-setup](group-setup/) | Slash commands for server admins to configure sharing, post channel, and spam threshold | Planned |
| [play-embed-posting](play-embed-posting/) | Post Discord embeds when plays are created in sharing-enabled groups; track posted messages in PlayPosts | Planned |

## Deferred (Design TBD)

| Batch | Notes |
|-------|-------|
| player-linking | Map Discord users to BGStats player UUIDs. Options: auto-link via meRefId on upload, message-command on embed for self-claim, manual tagging at upload time. Multiple Discord users per UUID may be acceptable. Workshop UX before planning. |
| file-distribution | DM linked players their play results after posting. Depends on player-linking design. |
| multi-play-threshold | Switch from individual embeds to a summary embed when a single upload exceeds the group's SpamThreshold. Depends on play-embed-posting being validated in production. |

## Archive

| Batch | Description |
|-------|-------------|
| [phase-2-upload-simplification](archive/phase-2-upload-simplification/) | Removed Contracts/auth, rebuilt upload endpoint with Wolverine event cascade (PlayExtracted → PlayCreated), colocated bot in Api |
