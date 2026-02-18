# Batch: discord-message-handler

## Goal
Handle `.bgsplay` file attachments dropped by users in Discord channels. The bot listens for
messages, downloads any `.bgsplay` attachments, parses them, and dispatches `PlayExtracted`
messages through Wolverine — the same pipeline used by the HTTP upload endpoint. The sender's
Discord ID is threaded through so plays can be attributed to a user. The bot reacts with ✅
after successful dispatch.

## Task Table

| ID | Task | Status | Depends On |
|----|------|--------|------------|
| 01 | [MessageReceivedHandler service](task-01-message-received-handler.md) | Completed | discord-bot-foundation |
| 02 | [Handle multiple .bgsplay attachments](task-02-multi-attachment.md) | Completed | 01 |

## Dependency Graph

```
01 ──► 02
```

## Acceptance Criteria

1. `dotnet build Hermod.slnx` — 0 errors
2. Dropping a `.bgsplay` file in any channel the bot can read triggers processing
3. The bot reacts with ✅ on the original message after dispatching
4. Plays from the file are persisted in the database
5. `Plays.UploadedById` is populated with the message author's linked user (if one exists)
6. Dropping a file containing multiple plays processes each play independently
7. Non-`.bgsplay` attachments and messages without attachments are ignored silently
8. Bot messages are never processed (no self-loops)
