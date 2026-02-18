# Task 02 — Remove UserEntity.DiscordId

## Why
`UserEntity.DiscordId` is superseded by `UserExternalLoginEntity`. Keeping both would create two
sources of truth for the same relationship. This task removes the column and its index so the
migration generated in Task 03 produces a clean diff.

## Steps

### 1. Remove `DiscordId` from `src/Hermod.Data/Entities/UserEntity.cs`

Delete the property:

```csharp
public ulong? DiscordId { get; set; }
```

The entity should look like:

```csharp
namespace Hermod.Data.Entities;

public class UserEntity
{
    public UserId Id { get; set; }
    public required string DisplayName { get; set; }
    public int? BggId { get; set; }
    public string? BggUsername { get; set; }
    public bool SubscribeToPlays { get; set; } = true;

    public List<UserGroupEntity> UserGroups { get; set; } = [];
    public List<PlayEntity> UploadedPlays { get; set; } = [];
    public List<PlayerMappingEntity> PlayerMappings { get; set; } = [];
    public List<UserExternalLoginEntity> ExternalLogins { get; set; } = [];
}
```

(The `ExternalLogins` nav property is added in Task 01.)

### 2. Update `src/Hermod.Data/Configurations/UserConfiguration.cs`

Remove the unique index line:

```csharp
builder.HasIndex(u => u.DiscordId).IsUnique().HasFilter("DiscordId IS NOT NULL");
```

The final `UserConfiguration.Configure` should be:

```csharp
public void Configure(EntityTypeBuilder<UserEntity> builder)
{
    builder.HasKey(u => u.Id);
    builder.Property(u => u.Id)
        .HasConversion(id => id.Value, v => UserId.From(v));

    builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
    builder.Property(u => u.BggUsername).HasMaxLength(200);
}
```

## Notes
- `BggId` / `BggUsername` remain on `UserEntity` for now because BGG identity is used for
  linking game records, not for login. They may move to `UserExternalLoginEntity` when BGG
  OAuth is added, but that's a separate decision.
- If any other code in the repo references `UserEntity.DiscordId`, update it during this task.
  A project-wide search for `DiscordId` will find any remaining references.

## Acceptance Criteria
- [ ] `UserEntity` has no `DiscordId` property
- [ ] `UserConfiguration` has no index on `DiscordId`
- [ ] No other code in the solution references `UserEntity.DiscordId`
- [ ] `dotnet build Hermod.slnx` succeeds
