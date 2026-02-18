# Task 04 — Remove Old Handlers and Mapper

## Why
`UploadPlaysCommand`, `UploadPlaysHandler`, and `ResponseMapper` belong to the old "one big handler"
pattern being replaced by the Wolverine event cascade. The new handlers (Tasks 09, 10) are written fresh.

## Steps

### 1. Delete files
```
src/Hermod.Api/Handlers/UploadPlaysCommand.cs
src/Hermod.Api/Handlers/UploadPlaysHandler.cs
src/Hermod.Api/Mappers/ResponseMapper.cs
src/Hermod.Api/Mappers/               (delete directory if now empty)
```

After Task 09 is complete, `src/Hermod.Api/Handlers/` will contain the new handlers.
`src/Hermod.Api/Mappers/` has no replacement and can be removed entirely.

## Acceptance Criteria
- `src/Hermod.Api/Handlers/` contains no files referencing `UploadPlaysCommand`
- `src/Hermod.Api/Mappers/` directory no longer exists
- No remaining files reference `Riok.Mapperly.Abstractions` or `[Mapper]`
