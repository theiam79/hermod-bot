# Task 08 — New UploadPlays Endpoint

## Why
Replaces the old `UploadPlays.cs` that did parse + persist + return in one shot.
The new version parses the file and dispatches one `PlayExtracted` message per play,
returning `202 Accepted` immediately. All persistence is handled downstream by handlers.

## Prerequisites
- **Task 07** must be complete (`PlayExtracted` message must exist)
- Task 03 should have deleted the old `UploadPlays.cs`

## Steps

Create `src/Hermod.Api/Endpoints/Plays/UploadPlays.cs`:

```csharp
using Hermod.Api.Messages;
using Hermod.BGStats;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public static class UploadPlays
{
    [WolverinePost("/api/plays/upload")]
    public static async Task<(AcceptedResponse, OutgoingMessages)> Post(
        IFormFile file,
        [FromQuery] Guid? groupId)
    {
        await using var stream = file.OpenReadStream();
        var result = await PlayFileParser.ParseAsync(stream);

        var messages = new OutgoingMessages();
        foreach (var play in result.Plays)
            messages.Add(new PlayExtracted(play, groupId));

        return (new AcceptedResponse(), messages);
    }
}
```

## Notes
- `AcceptedResponse` is from `Wolverine.Http` — already referenced. It returns HTTP 202.
- `OutgoingMessages` is from `Wolverine` — already referenced.
- No `[Authorize]` — auth is removed for now.
- `groupId` is optional. Plays uploaded without a group are still persisted (user's personal plays).
- The endpoint does not touch the database — parsing only. All DB work is in `PlayExtractedHandler`.
- Wolverine dispatches the `OutgoingMessages` after the HTTP response is sent.

## Acceptance Criteria
- `POST /api/plays/upload?groupId=<optional-guid>` with a multipart file returns `202 Accepted`
- The response body is empty (AcceptedResponse writes no body)
- One `PlayExtracted` message is dispatched per play in the file
- No database writes happen in this endpoint
