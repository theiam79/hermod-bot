# Hermod Bot — Handover

_Last updated: 2026-02-18_

## What Has Been Built

The core end-to-end play-sharing pipeline is complete. Dropping a `.bgsplay` file into any Discord channel the bot can read results in:

1. Bot downloads the attachment and parses it (BGStats format)
2. Play data is written to SQLite via EF Core
3. A Discord embed is posted to the configured channel (if the server has opted in)
4. The posted message ID is recorded in `PlayPosts` (unique per guild per play)

### Completed Batches

| Batch | What it Delivered |
|-------|-------------------|
| `phase-2-upload-simplification` | Wolverine event cascade (PlayExtracted → PlayCreated → PostPlay), colocated bot in Api |
| `discord-bot-foundation` | Discord.Net hosted services wired into Hermod.Api; slash commands; guild sync on startup |
| `user-identity` | `UserExternalLoginEntity` table; find-or-create user from Discord ID on upload |
| `discord-message-handler` | Detect `.bgsplay` attachments in any channel; parse and dispatch; react ✅/❌ |
| `group-setup` | `/hermod allow-sharing`, `/hermod set-channel`, `/hermod set-threshold` slash commands |
| `play-embed-posting` | `PlayPostEntity` + migration; `PostPlayHandler` builds embed and posts to Discord |

### Current Data Model

- **Users** — one row per Discord user (created on first upload)
- **UserExternalLogins** — Discord OAuth credentials (Provider + ProviderKey)
- **Groups** — one row per Discord guild (upserted on bot join/ready); holds `AllowSharing`, `DiscordPostChannelId`, `SpamThreshold`
- **Plays** — one row per parsed play
- **PlayPlayers** — players within a play (score, rank, winner, role, team)
- **PlayerMappings** — BGStats UUID → UserId mapping (schema exists, not yet populated)
- **PlayPosts** — record of every Discord embed posted (unique index on PlayId + DiscordGuildId)

---

## Human Setup Required to Test

The following steps must be done manually before the app can run.

### 1. Discord Developer Portal

1. Go to https://discord.com/developers/applications and create (or open) the application
2. Under **Bot**:
   - Copy the **Token** — you'll need it below
   - Enable **Privileged Gateway Intents**:
     - ✅ Server Members Intent (`GatewayIntents.GuildMembers`)
     - ✅ Message Content Intent (`GatewayIntents.MessageContent`)
3. Under **OAuth2 → URL Generator**: scope `bot` + `applications.commands`, permissions `Send Messages` + `Read Message History` + `View Channels`
4. Use the generated URL to invite the bot to your test server

### 2. Discord Token — User Secret

The Aspire AppHost passes the token to the API as an environment variable. Set it via user secrets on the **AppHost** project:

```bash
dotnet user-secrets set "Parameters:discord-token" "<your-token>" \
  --project src/Hermod.AppHost/Hermod.AppHost.csproj
```

> The key is `Parameters:discord-token` (not `Discord:Token`) because Aspire resolves it as a named parameter.

### 3. Run

```bash
~/.aspire/bin/aspire run --project src/Hermod.AppHost/Hermod.AppHost.csproj
```

Aspire starts Hermod.Api (which contains the bot). On startup:
- EF Core migrations are applied automatically
- The bot connects and logs `Connected as <BotName>`
- On `Client.Ready`, all guilds are upserted into `Groups` (with `AllowSharing = false`)

### 4. Configure a Test Server

Once the bot is running and in your test server, run these slash commands (requires Manage Server permission):

```
/hermod allow-sharing enabled:True
/hermod set-channel channel:#your-channel
```

> Global slash commands can take up to 1 hour to propagate. For faster testing during development, register commands to a specific guild instead (change `RegisterCommandsGloballyAsync` in `InteractionHandler.cs` to `RegisterCommandsToGuildAsync`).

### 5. Test the Pipeline

Drop a `.bgsplay` file (from BGStats export) into any channel the bot can read. The bot will:
- React ✅ when done
- Post an embed in the configured channel
- React ❌ if all files fail to parse

Sample `.bgsplay` files can be exported from the BGStats iOS/Android app under Settings → Export.

---

## Deferred Batches (Design TBD)

These are tracked in `tasks/TASKS.md` as deferred. No task files exist yet — design decisions are needed first.

### `player-linking`

Map Discord users to BGStats player UUIDs within a play so the embed can @-mention real users.

Options discussed (no decision yet):
- **Auto-link via `meRefId`**: BGStats marks the device owner's player with a UUID; on upload, link that player to the uploading Discord user automatically
- **Self-claim via message command**: Add a message command (right-click → Apps) on the embed so Discord users can claim themselves as a player
- **Manual tagging**: Allow users to tag themselves at upload time

Multiple Discord users per BGStats UUID may be valid (e.g. two people sharing a device). Worth workshopping before planning.

### `file-distribution`

After a play embed is posted, DM all linked players their individual result. Depends on `player-linking` being implemented first.

### `multi-play-threshold`

When a single upload contains ≥ `SpamThreshold` plays (default 3), post a single summary embed instead of individual embeds per play. `SpamThreshold` is already stored per group and configurable via `/hermod set-threshold`. Implement after individual embeds have been validated in production use.

---

## Architectural Notes for Future Sessions

- **Bot is colocated in Hermod.Api** — there is no separate `Hermod.Bot` project. The original architecture doc described a separated approach; the current codebase uses colocated hosting.
- **Wolverine cascade pattern**: `PlayExtractedHandler` → returns `PlayCreated` → `PlayCreatedHandler` returns `PostPlay?` → `PostPlayHandler`. All within one Wolverine unit of work backed by EF Core transactions (`AutoApplyTransactions()`). No manual `SaveChangesAsync()` calls needed in handlers.
- **Discord module pattern**: `InfoModule` uses `partial class` split across `InfoModule.cs` and `InfoModule.Admin.cs` to keep all `/hermod` subcommands under one registered type. Do not split into separate classes — Discord.Net would require separate group registrations.
- **Singleton → Scoped access**: All Discord hosted services (`DiscordClientService` subclasses) are singletons. When they need EF Core (`HermodContext` is scoped), inject `IServiceScopeFactory` and `CreateAsyncScope()` per operation.
- **`global::` required**: Within `Hermod.Api.Discord`, `IResult` must be written as `global::Discord.Interactions.IResult` to avoid ambiguity with `Microsoft.AspNetCore.Http.IResult`.

---

## Next Session Checklist

- [ ] Test the end-to-end pipeline with a real `.bgsplay` file (requires human setup above)
- [ ] Validate embed appearance and content against actual BGStats data
- [ ] Decide player-linking UX approach, then plan the `player-linking` batch
- [ ] Consider whether `multi-play-threshold` is needed before player-linking (can be implemented independently)
