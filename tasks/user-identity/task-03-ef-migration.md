# Task 03 — Add EF Migration

## Why
The schema changes from Tasks 01 and 02 (new `UserExternalLogins` table, dropped `DiscordId`
column) must be captured as an EF Core migration so they apply consistently across all
environments.

## Steps

### 1. Run the migration command

```bash
dotnet ef migrations add AddUserExternalLogins \
    --project src/Hermod.Data \
    --startup-project src/Hermod.Api
```

This generates:
- `src/Hermod.Data/Migrations/<timestamp>_AddUserExternalLogins.cs`
- Updated `src/Hermod.Data/Migrations/HermodContextModelSnapshot.cs`

### 2. Verify the migration content

Open the generated migration file and confirm it contains:

**Up:**
- `CreateTable("UserExternalLogins", ...)` with columns: `Id`, `UserId`, `Provider`, `ProviderKey`
- `AddForeignKey` from `UserExternalLogins.UserId` → `Users.Id` (cascade delete)
- `CreateIndex` on `(Provider, ProviderKey)` — unique
- `DropColumn("DiscordId", "Users")`

**Down:**
- `DropTable("UserExternalLogins")`
- `AddColumn("DiscordId", "Users", nullable: true)`
- Recreate the `DiscordId` unique index

If any of these are missing or the migration contains unexpected changes, investigate before
proceeding.

### 3. Confirm the app applies the migration on startup

`Hermod.Api` is configured to call `MigrateAsync()` at startup. Run the app once (without a
Discord token, or with a dummy token) and confirm the migration runs without error:

```bash
dotnet run --project src/Hermod.Api
```

Look for log output similar to:
```
Applying migration '20260218_AddUserExternalLogins'
```

## Notes
- The `--startup-project` flag is required because `Hermod.Data` has no connection string of
  its own — it gets one from the API via `DesignTimeDbContextFactory`.
- Do not manually edit the generated migration file unless something is genuinely wrong.
  Re-running `dotnet ef migrations add` is safer.
- The SQLite database file is typically `hermod.db` in the working directory when running
  locally. Delete it to start fresh if you want to verify the full migration sequence.

## Acceptance Criteria
- [ ] Migration file `*_AddUserExternalLogins.cs` exists in `src/Hermod.Data/Migrations/`
- [ ] Migration Up creates `UserExternalLogins` table and drops `DiscordId` column from `Users`
- [ ] Migration Down is the inverse (drops table, restores column)
- [ ] `dotnet build Hermod.slnx` succeeds after migration is generated
- [ ] App starts and migration applies without error
