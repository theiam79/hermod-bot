# Task 09 — PlayExtractedHandler

## Why
Handles the `PlayExtracted` message for each individual play. Responsible for building and
persisting the `PlayEntity` + `PlayPlayerEntity` records. Cascades `PlayCreated` on success.
Wolverine's auto-transaction wraps the handler — no explicit `SaveChangesAsync` needed.

## Prerequisites
- **Task 05** must be complete (`PlayEntity.UploadedById` is nullable, `RawPlayFileJson` is gone)
- **Task 07** must be complete (`PlayExtracted` and `PlayCreated` messages exist)

## Steps

Create `src/Hermod.Api/Handlers/PlayExtractedHandler.cs`:

```csharp
using Hermod.Api.Messages;
using Hermod.BGStats.Models;
using Hermod.Data;
using Hermod.Data.Entities;

namespace Hermod.Api.Handlers;

public static class PlayExtractedHandler
{
    public static PlayCreated Handle(PlayExtracted message, HermodContext db)
    {
        var play = message.ParsedPlay;

        var entity = new PlayEntity
        {
            Id = PlayId.From(Guid.NewGuid()),
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
}
```

## Notes
- Wolverine discovers this as a handler via the `Handle` method name convention.
- The `PlayCreated` return value is automatically cascaded by Wolverine (no explicit `bus.PublishAsync`).
- `db.Plays.Add(entity)` without `SaveChangesAsync` — Wolverine's `AutoApplyTransactions` commits
  after the handler returns.
- Each `PlayExtracted` message is handled independently. If one play fails, others are not affected.
- `UploadedById` is not set (null) — no user management yet. Will be added with auth in Phase 4.
- Player mapping resolution (linking BGStats UUIDs to platform users) is not implemented here yet.
  `MappedUserId` stays null — this is a future concern.

## Acceptance Criteria
- Wolverine routes `PlayExtracted` messages to this handler (confirmed by handler naming convention)
- For each `PlayExtracted`, one `PlayEntity` + N `PlayPlayerEntity` rows are persisted
- `PlayCreated` is cascaded after successful persistence
- Handler has no explicit `SaveChangesAsync` call
- No player mapping resolution (deferred)
