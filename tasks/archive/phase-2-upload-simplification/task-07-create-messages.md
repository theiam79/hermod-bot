# Task 07 — Create PlayExtracted and PlayCreated Messages

## Why
These are the Wolverine messages that form the upload event cascade:
- `PlayExtracted` — dispatched once per play in the uploaded file; carries parsed play data
- `PlayCreated` — cascaded after a play is persisted; will trigger Discord notifications

Both are simple records with no framework dependencies.

## Steps

Create directory `src/Hermod.Api/Messages/` and add two files:

### `src/Hermod.Api/Messages/PlayExtracted.cs`
```csharp
using Hermod.BGStats.Models;

namespace Hermod.Api.Messages;

public record PlayExtracted(Play ParsedPlay, Guid? GroupId);
```

### `src/Hermod.Api/Messages/PlayCreated.cs`
```csharp
namespace Hermod.Api.Messages;

public record PlayCreated(Guid PlayId, Guid? GroupId);
```

## Notes
- `Play` is from `Hermod.BGStats.Models` — already referenced by `Hermod.Api.csproj`
- `Guid?` is used throughout (not Vogen) — messages cross handler boundaries and plain Guids
  are simpler. Vogen IDs live in the Data layer.
- These types are internal to `Hermod.Api` — no external project references them.

## Acceptance Criteria
- `src/Hermod.Api/Messages/PlayExtracted.cs` exists with the record above
- `src/Hermod.Api/Messages/PlayCreated.cs` exists with the record above
- Both compile as part of `Hermod.Api`
