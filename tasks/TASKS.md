# Task Tracker

## Active Batches

| Batch | Description | Status |
|-------|-------------|--------|
| *(none)* | | |

## Deferred (Design TBD)

| Batch | Notes |
|-------|-------|
| multi-play-threshold | Switch from individual embeds to a summary embed when a single upload exceeds the group's SpamThreshold. Depends on play-embed-posting being validated in production. |

## Archive

| Batch | Description |
|-------|-------------|
| [file-distribution](archive/file-distribution/) | DM linked players their .bgsplay file for BGStats import; UploadEntity, pipeline refactor, per-player distribution |
| [player-linking](archive/player-linking/) | Map Discord users to BGStats player UUIDs via meRefId auto-link, message command self-claim, and /hermod my-players |
| [play-embed-posting](archive/play-embed-posting/) | Post Discord embeds when plays are created in sharing-enabled groups; track posted messages in PlayPosts |
| [group-setup](archive/group-setup/) | Slash commands for server admins to configure sharing, post channel, and spam threshold |
| [discord-message-handler](archive/discord-message-handler/) | Process .bgsplay attachments dropped in Discord channels; react ✅ and dispatch through Wolverine pipeline |
| [user-identity](archive/user-identity/) | Replace UserEntity.DiscordId with UserExternalLoginEntity table; wire find-or-create into upload flow |
| [discord-bot-foundation](archive/discord-bot-foundation/) | Wire Discord.Net into Hermod.Api as hosted services; connect bot, register slash commands, sync guilds |
| [phase-2-upload-simplification](archive/phase-2-upload-simplification/) | Removed Contracts/auth, rebuilt upload endpoint with Wolverine event cascade (PlayExtracted → PlayCreated), colocated bot in Api |
