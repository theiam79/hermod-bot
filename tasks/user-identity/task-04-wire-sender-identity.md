# Task 04 — Wire Sender Identity into Upload Flow

## Why
Plays uploaded via the Discord bot (or the future mobile share intent) carry a sender identity.
This task adds a `SenderDiscordId` field to the `PlayExtracted` message, threads it through
the upload endpoint, and implements find-or-create logic in `PlayExtractedHandler` so that
`PlayEntity.UploadedById` is populated whenever a Discord user ID is known.

## Steps

### 1. Update `src/Hermod.Api/Messages/PlayExtracted.cs`

```csharp
using Hermod.BGStats.Models;

namespace Hermod.Api.Messages;

/// <param name="SenderDiscordId">
/// Discord user ID of the uploader as a string, or null if the sender is unknown.
/// Stored as string to avoid ulong serialization issues.
/// </param>
public record PlayExtracted(Play ParsedPlay, Guid? GroupId, string? SenderDiscordId);
```

### 2. Update `src/Hermod.Api/Endpoints/Plays/UploadPlays.cs`

Add `senderDiscordId` as an optional query parameter and pass it into each `PlayExtracted`
message:

```csharp
[WolverinePost("/api/plays/upload")]
public static async Task<(IResult, OutgoingMessages)> Post(
    IFormFile file,
    [FromQuery] Guid? groupId,
    [FromQuery] string? senderDiscordId)
{
    await using var stream = file.OpenReadStream();
    var result = await PlayFileParser.ParseAsync(stream);

    var messages = new OutgoingMessages();
    foreach (var play in result.Plays)
        messages.Add(new PlayExtracted(play, groupId, senderDiscordId));

    return (Results.Accepted(), messages);
}
```

### 3. Update `src/Hermod.Api/Handlers/PlayExtractedHandler.cs`

Inject `HermodContext` (already present) and add find-or-create logic before building
the `PlayEntity`:

```csharp
using Hermod.Api.Messages;
using Hermod.BGStats.Models;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Api.Handlers;

public static class PlayExtractedHandler
{
    public static async Task<PlayCreated> Handle(PlayExtracted message, HermodContext db)
    {
        var play = message.ParsedPlay;

        UserId? uploadedById = null;
        if (message.SenderDiscordId is not null)
            uploadedById = await FindOrCreateUserAsync(db, message.SenderDiscordId);

        var entity = new PlayEntity
        {
            Id = PlayId.From(Guid.NewGuid()),
            UploadedById = uploadedById,
            GroupId = message.GroupId.HasValue ? GroupId.From(message.GroupId.Value) : null,
            BgStatsPlayUuid = play.Uuid.ToString(),
            GameName = play.Game.Name,
            BggGameId = play.Game.BggId > 0 ? play.Game.BggId : null,
            GameThumbnailUrl = string.IsNullOrEmpty(play.Game.ThumbnailUrl) ? null : play.Game.ThumbnailUrl,
            DatePlayed = play.DatePlayed,
            Duration = play.Duration > TimeSpan.Zero ? play.Duration : null,
            LocationName = string.IsNullOrEmpty(play.Location.Name) ? null : play.Location.Name,
            Rounds = play.Rounds > 0 ? play.Rounds : null,
            Comments = play.Comments,
            CreatedAt = DateTime.UtcNow,
            Players = play.Scores.Select(s => new PlayPlayerEntity
            {
                Id = PlayPlayerId.From(Guid.NewGuid()),
                BgStatsPlayerUuid = s.Player.Uuid.ToString(),
                PlayerName = s.Player.Name,
                Score = s.ScoreExpression,
                CalculatedScore = s.CalculateScore(),
                Winner = s.Winner,
                Rank = s.Rank > 0 ? s.Rank : null,
                Role = string.IsNullOrEmpty(s.Role) ? null : s.Role,
                Team = s.Team,
                NewPlayer = s.NewPlayer,
                StartPlayer = s.StartPlayer,
            }).ToList(),
        };

        db.Plays.Add(entity);

        return new PlayCreated(entity.Id.Value, message.GroupId);
    }

    private static async Task<UserId> FindOrCreateUserAsync(HermodContext db, string discordId)
    {
        const string provider = "Discord";

        var login = await db.UserExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == discordId);

        if (login is not null)
            return login.UserId;

        var user = new UserEntity
        {
            Id = UserId.From(Guid.NewGuid()),
            DisplayName = $"Discord:{discordId}",   // placeholder until profile is fetched
        };

        db.Users.Add(user);

        db.UserExternalLogins.Add(new UserExternalLoginEntity
        {
            Id = ExternalLoginId.From(Guid.NewGuid()),
            UserId = user.Id,
            Provider = provider,
            ProviderKey = discordId,
        });

        return user.Id;
    }
}
```

## Notes
- `Handle` is now `async Task<PlayCreated>` — Wolverine supports async handlers, so this is
  fine. Remove the `static` modifier from the original sync version if it was there.
  (The class is still `public static class`.)
- `DisplayName` is set to `"Discord:<id>"` as a placeholder. A subsequent batch
  (`discord-bot-foundation` is already planned) will resolve the Discord username via
  `IGuild.GetUserAsync` and update it.
- **Security note**: `senderDiscordId` is accepted as an unauthenticated query parameter for
  now. This means any caller can claim to be any Discord user. This is intentional and
  temporary — proper auth (Phase 4) will replace it with session-derived identity. Do not use
  this parameter for authorization decisions in the meantime.
- The find-or-create is inside the same Wolverine handler unit of work. Wolverine's
  `AutoApplyTransactions()` ensures both the user, the external login, and the play are
  committed atomically.

## Acceptance Criteria
- [ ] `PlayExtracted` record has `string? SenderDiscordId` field
- [ ] `UploadPlays.Post` accepts `[FromQuery] string? senderDiscordId` and passes it through
- [ ] `PlayExtractedHandler` performs find-or-create on `UserExternalLogins` when `SenderDiscordId` is set
- [ ] `dotnet build Hermod.slnx` succeeds
- [ ] `POST /api/plays/upload?senderDiscordId=12345` creates a `Users` row and a `UserExternalLogins` row
- [ ] A second upload with the same `senderDiscordId` reuses the existing user (no duplicate rows)
- [ ] `Plays.UploadedById` is populated when `senderDiscordId` is provided; null otherwise
