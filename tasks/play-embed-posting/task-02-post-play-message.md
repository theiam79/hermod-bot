# Task 02 — PostPlay Message and PlayCreatedHandler Update

## Why
`PlayCreatedHandler` currently just logs. This task promotes it to dispatch a `PostPlay`
command when the play belongs to a group — which is the trigger for embed posting. The
command carries only IDs; the handler in Task 03 fetches the full data it needs from the DB.

## Steps

### Create `src/Hermod.Api/Messages/PostPlay.cs`

```csharp
namespace Hermod.Api.Messages;

/// <summary>
/// Dispatched after a play is persisted. Instructs the embed posting handler to
/// check group settings and post a Discord embed if appropriate.
/// </summary>
public record PostPlay(Guid PlayId, Guid GroupId);
```

### Update `src/Hermod.Api/Handlers/PlayCreatedHandler.cs`

```csharp
using Hermod.Api.Messages;
using Microsoft.Extensions.Logging;

namespace Hermod.Api.Handlers;

public static class PlayCreatedHandler
{
    public static PostPlay? Handle(PlayCreated message, ILogger logger)
    {
        logger.LogInformation("Play {PlayId} created for group {GroupId}",
            message.PlayId, message.GroupId);

        if (!message.GroupId.HasValue)
            return null;   // no group context — skip posting

        return new PostPlay(message.PlayId, message.GroupId.Value);
    }
}
```

When `Handle` returns `null`, Wolverine does not cascade any message. When it returns a
`PostPlay` instance, Wolverine cascades it to `PostPlayHandler` (Task 03).

## Notes
- `PlayCreated.GroupId` is `Guid?`. If null (anonymous upload with no group), no embed is
  posted — the play is still persisted, but distribution is skipped.
- This handler remains synchronous. The actual I/O (DB read + Discord API call) happens in
  `PostPlayHandler`, keeping concerns separated.
- Wolverine's `AutoApplyTransactions()` ensures `PlayExtractedHandler` (which writes the
  `PlayEntity`) commits before `PlayCreatedHandler` runs. `PostPlayHandler` can safely read
  the play from the DB.

## Acceptance Criteria
- [ ] `src/Hermod.Api/Messages/PostPlay.cs` exists
- [ ] `PlayCreatedHandler.Handle` returns `PostPlay` when `GroupId` is set, `null` when not
- [ ] `dotnet build Hermod.slnx` succeeds
- [ ] No regression: plays without a GroupId are still persisted (no exception thrown)
