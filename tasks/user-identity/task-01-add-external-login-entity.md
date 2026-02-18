# Task 01 — Add UserExternalLoginEntity

## Why
A dedicated external-login table stores provider/key pairs independently of the `UserEntity`
itself. This mirrors the `AspNetUserLogins` shape used by ASP.NET Identity so that adding full
Identity support later requires a data migration rather than a structural redesign. It also
naturally supports multiple providers per user (Discord + BGG + future providers) without
adding nullable columns to `UserEntity`.

## Steps

### 1. Add `ExternalLoginId` to `src/Hermod.Data/Ids.cs`

```csharp
[ValueObject<Guid>]
public readonly partial struct ExternalLoginId;
```

### 2. Create `src/Hermod.Data/Entities/UserExternalLoginEntity.cs`

```csharp
namespace Hermod.Data.Entities;

public class UserExternalLoginEntity
{
    public ExternalLoginId Id { get; set; }
    public UserId UserId { get; set; }

    /// <summary>Provider name, e.g. "Discord", "BGG".</summary>
    public required string Provider { get; set; }

    /// <summary>Provider's unique key for the user (Discord: ulong as string; BGG: int as string).</summary>
    public required string ProviderKey { get; set; }

    public UserEntity User { get; set; } = null!;
}
```

### 3. Create `src/Hermod.Data/Configurations/UserExternalLoginConfiguration.cs`

```csharp
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLoginEntity>
{
    public void Configure(EntityTypeBuilder<UserExternalLoginEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => ExternalLoginId.From(v));

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, v => UserId.From(v));

        builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ProviderKey).IsRequired().HasMaxLength(200);

        builder.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany(u => u.ExternalLogins)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 4. Add nav property to `src/Hermod.Data/Entities/UserEntity.cs`

```csharp
public List<UserExternalLoginEntity> ExternalLogins { get; set; } = [];
```

### 5. Add `DbSet` to `src/Hermod.Data/HermodContext.cs`

```csharp
public DbSet<UserExternalLoginEntity> UserExternalLogins => Set<UserExternalLoginEntity>();
```

## Notes
- `ProviderKey` is always stored as a string. For Discord, convert the `ulong` to string
  (`discordId.ToString()`). This avoids numeric type issues across providers.
- The unique index on `(Provider, ProviderKey)` enforces that each provider account maps to
  exactly one `UserEntity`. It is the lookup key for find-or-create.
- `Provider` values should use simple, consistent strings: `"Discord"`, `"BGG"`. No enums —
  strings are easier to extend and store without migrations.

## Acceptance Criteria
- [ ] `ExternalLoginId` struct exists in `Ids.cs`
- [ ] `src/Hermod.Data/Entities/UserExternalLoginEntity.cs` exists
- [ ] `src/Hermod.Data/Configurations/UserExternalLoginConfiguration.cs` exists
- [ ] `UserEntity` has `List<UserExternalLoginEntity> ExternalLogins` nav property
- [ ] `HermodContext` has `DbSet<UserExternalLoginEntity> UserExternalLogins`
- [ ] `dotnet build src/Hermod.Data/Hermod.Data.csproj` succeeds
