# Task Tracker

## Active Batches

| Batch | Description | Status |
|-------|-------------|--------|
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
| [discord-message-handler](archive/discord-message-handler/) | Process .bgsplay attachments dropped in Discord channels; react ✅ and dispatch through Wolverine pipeline |
| [user-identity](archive/user-identity/) | Replace UserEntity.DiscordId with UserExternalLoginEntity table; wire find-or-create into upload flow |
| [discord-bot-foundation](archive/discord-bot-foundation/) | Wire Discord.Net into Hermod.Api as hosted services; connect bot, register slash commands, sync guilds |
| [phase-2-upload-simplification](archive/phase-2-upload-simplification/) | Removed Contracts/auth, rebuilt upload endpoint with Wolverine event cascade (PlayExtracted → PlayCreated), colocated bot in Api |
