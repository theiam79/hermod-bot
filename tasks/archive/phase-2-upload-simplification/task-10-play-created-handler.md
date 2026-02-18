# Task 10 — PlayCreatedHandler Stub

## Why
`PlayCreated` is the event that will eventually trigger Discord embed notifications. For now,
a stub handler with logging establishes the pattern and confirms the cascade is wired correctly.

## Prerequisites
- **Task 07** must be complete (`PlayCreated` message must exist)

## Steps

Create `src/Hermod.Api/Handlers/PlayCreatedHandler.cs`:

```csharp
using Hermod.Api.Messages;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class PlayCreatedHandler
{
    public static void Handle(PlayCreated message, ILogger<PlayCreatedHandler> logger)
    {
        logger.LogInformation(
            "Play {PlayId} created in group {GroupId}",
            message.PlayId,
            message.GroupId);
    }
}
```

## Notes
- Wolverine injects `ILogger<T>` directly into handler parameters — no constructor needed.
- This handler intentionally does nothing beyond logging. It is a placeholder for Phase 3
  Discord notification logic.
- When Discord notification is added, this handler will call a Discord service to post an embed
  to the group's configured channel.

## Acceptance Criteria
- Wolverine routes `PlayCreated` messages to this handler
- A log line at Information level appears after each play is processed
- Handler has no side effects beyond logging
