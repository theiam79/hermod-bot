# Task 01 — Add SpamThreshold to GroupEntity

## Why
`SpamThreshold` controls how many plays from a single upload can be posted as individual
embeds before the bot switches to a single summary embed. It lives on `GroupEntity` so each
server can tune it independently.

## Steps

### 1. Add property to `src/Hermod.Data/Entities/GroupEntity.cs`

```csharp
/// <summary>
/// Number of plays in a single upload at or above which a summary embed is posted
/// instead of individual embeds. Defaults to 3.
/// </summary>
public int SpamThreshold { get; set; } = 3;
```

### 2. Update `src/Hermod.Data/Configurations/GroupConfiguration.cs`

No special EF configuration is required for an `int` column with a default — EF Core will
use the CLR default from the property initializer. Verify the configuration file does not
need changes. If a database-level default is preferred, add:

```csharp
builder.Property(g => g.SpamThreshold).HasDefaultValue(3);
```

### 3. Add EF migration

```bash
dotnet ef migrations add AddGroupSpamThreshold \
    --project src/Hermod.Data \
    --startup-project src/Hermod.Api
```

Verify the generated migration Up adds `SpamThreshold INTEGER NOT NULL DEFAULT 3` to the
`Groups` table, and Down drops it.

## Notes
- The default of 3 means: 1–2 plays → individual embeds; 3+ plays → summary embed.
  This can be adjusted per-guild via the slash command in Task 02.
- `GuildHandler` (discord-bot-foundation task-05) creates new `GroupEntity` rows with no
  explicit `SpamThreshold` — the CLR default of 3 applies.
- Existing rows in the database (after migration) will also get 3 as the default, set by the
  SQLite column default.

## Acceptance Criteria
- [ ] `GroupEntity` has `int SpamThreshold { get; set; } = 3`
- [ ] Migration `*_AddGroupSpamThreshold.cs` exists and adds the column with default 3
- [ ] `dotnet build Hermod.slnx` succeeds
- [ ] App starts and migration applies without error
