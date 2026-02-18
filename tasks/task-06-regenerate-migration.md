# Task 06 — Regenerate EF Core Migration

## Why
The existing `InitialCreate` migration reflects the old schema (non-nullable `UploadedById`,
`RawPlayFileJson` column). Since no production database exists, the cleanest approach is to
delete the migration and regenerate it from the updated model.

## Prerequisites
- **Task 05** must be complete (PlayEntity updated)
- All other code changes that affect the Data model should also be done before this task,
  since the migration will be generated from the current state of all entity configurations.

## Steps

### 1. Delete existing migration files
```
src/Hermod.Data/Migrations/20260217040658_InitialCreate.cs
src/Hermod.Data/Migrations/20260217040658_InitialCreate.Designer.cs
src/Hermod.Data/Migrations/HermodContextModelSnapshot.cs
```

### 2. Regenerate
Run from the repo root:
```bash
dotnet ef migrations add InitialCreate \
  --project src/Hermod.Data \
  --startup-project src/Hermod.Api \
  --output-dir Migrations
```

The `DesignTimeDbContextFactory` in `Hermod.Data` provides the DbContext for design-time tooling.
The startup project (`Hermod.Api`) provides the connection string configuration.

## Acceptance Criteria
- `src/Hermod.Data/Migrations/` contains a new `*_InitialCreate.cs` and snapshot
- The generated migration does **not** contain a `RawPlayFileJson` column on `Plays`
- The generated migration has `UploadedById` as nullable on `Plays`
- `dotnet build Hermod.slnx` succeeds after regeneration
