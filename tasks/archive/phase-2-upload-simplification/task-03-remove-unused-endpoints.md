# Task 03 — Remove Unused Endpoints

## Why
All endpoints except play upload are premature. Groups, Users, PlayerMappings, GetPlay, and
ListPlays will be re-added incrementally once the core upload flow is solid and tested.

## Steps

### 1. Delete endpoint directories and files
```
src/Hermod.Api/Endpoints/Groups/          (delete entire directory)
src/Hermod.Api/Endpoints/Users/           (delete entire directory)
src/Hermod.Api/Endpoints/PlayerMappings/  (delete entire directory)
src/Hermod.Api/Endpoints/Plays/GetPlay.cs
src/Hermod.Api/Endpoints/Plays/ListPlays.cs
src/Hermod.Api/Endpoints/Plays/UploadPlays.cs  (old version — replaced in Task 08)
```

After deletion, `src/Hermod.Api/Endpoints/Plays/` should be empty (or contain only the new
`UploadPlays.cs` created in Task 08 — ordering doesn't matter).

## Notes
- The `[Authorize]` attribute used in these files references `ApiKeyDefaults` which is also
  being removed (Task 02). Deleting these files removes all `[Authorize]` usage.
- `ResponseMapper` (which maps to Contracts DTOs) is removed separately in Task 04.

## Acceptance Criteria
- Only `src/Hermod.Api/Endpoints/Plays/` directory remains under `Endpoints/`
- No remaining files reference `Hermod.Contracts` types (GroupResponse, UserResponse, etc.)
- No remaining files have `[Authorize]` attributes
