# Batch: discord-bot-foundation

## Goal
Establish Discord.Net as a set of hosted services inside `Hermod.Api`. The bot connects to
Discord, registers slash commands, and keeps `GroupEntity` in sync with the guilds it belongs
to. This is the foundation every Discord-facing feature depends on.

## Task Table

| ID | Task | Status | Depends On |
|----|------|--------|------------|
| 01 | [Add Discord.Net packages and config](task-01-packages-and-config.md) | Open | — |
| 02 | [Register Discord client in DI](task-02-register-discord-client.md) | Open | 01 |
| 03 | [BotService — connection and logging](task-03-bot-service.md) | Open | 02 |
| 04 | [InteractionHandler — slash command routing](task-04-interaction-handler.md) | Open | 03 |
| 05 | [GuildHandler — guild sync](task-05-guild-handler.md) | Open | 03 |

## Dependency Graph

```
01 ──► 02 ──► 03 ──► 04
                └──► 05
```

Tasks 04 and 05 are independent of each other and can be done in parallel after 03.

## Acceptance Criteria

1. `dotnet build Hermod.slnx` — 0 errors
2. App starts and logs show `Connected as <BotName>`
3. Bot appears online in Discord
4. `/hermod ping` responds `Pong!` in Discord
5. `Groups` table contains one row per guild the bot is in after startup
6. Joining the bot to a new server adds a row with `AllowSharing = false`
7. Leaving a server logs a message but does not delete the row
