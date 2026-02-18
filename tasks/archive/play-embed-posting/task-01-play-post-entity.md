# Task 01 — PlayPostEntity and Migration

## Why
`PlayPostEntity` tracks every Discord message Hermod posts so that embeds can be edited (e.g.
when a play is updated) or deleted (e.g. when a play is removed) in a later batch. One row
is created per guild per play — a play shared to multiple servers in the future would produce
multiple rows.

## Steps

### 1. Add `PlayPostId` to `src/Hermod.Data/Ids.cs`

```csharp
[ValueObject<Guid>]
public readonly partial struct PlayPostId;
```

### 2. Create `src/Hermod.Data/Entities/PlayPostEntity.cs`

```csharp
namespace Hermod.Data.Entities;

public class PlayPostEntity
{
    public PlayPostId Id { get; set; }
    public PlayId PlayId { get; set; }
    public ulong DiscordGuildId { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ulong DiscordMessageId { get; set; }
    public DateTime PostedAt { get; set; }

    public PlayEntity Play { get; set; } = null!;
}
```

### 3. Create `src/Hermod.Data/Configurations/PlayPostConfiguration.cs`

```csharp
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class PlayPostConfiguration : IEntityTypeConfiguration<PlayPostEntity>
{
    public void Configure(EntityTypeBuilder<PlayPostEntity> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, v => PlayPostId.From(v));

        builder.Property(p => p.PlayId)
            .HasConversion(id => id.Value, v => PlayId.From(v));

        builder.HasOne(p => p.Play)
            .WithMany()
            .HasForeignKey(p => p.PlayId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enforce one post per guild per play
        builder.HasIndex(p => new { p.PlayId, p.DiscordGuildId }).IsUnique();
    }
}
```

### 4. Add nav property to `src/Hermod.Data/Entities/PlayEntity.cs`

```csharp
public List<PlayPostEntity> Posts { get; set; } = [];
```

### 5. Add `DbSet` to `src/Hermod.Data/HermodContext.cs`

```csharp
public DbSet<PlayPostEntity> PlayPosts => Set<PlayPostEntity>();
```

### 6. Add EF migration

```bash
dotnet ef migrations add AddPlayPosts \
    --project src/Hermod.Data \
    --startup-project src/Hermod.Api
```

Verify the migration creates a `PlayPosts` table with the unique index on `(PlayId, DiscordGuildId)`.

## Notes
- No `UserId` on `PlayPostEntity` — the uploader is already on `PlayEntity.UploadedById`.
  If per-post author tracking is needed later, it can be added then.
- The unique index on `(PlayId, DiscordGuildId)` enforces that a play is posted at most once
  per guild. This will need revisiting if multi-server posting is ever supported intentionally.
- `OnDelete(DeleteBehavior.Cascade)` — deleting a play deletes its post tracking rows. The
  Discord messages themselves would need to be cleaned up in application code before the
  delete.

## Acceptance Criteria
- [ ] `PlayPostId` exists in `Ids.cs`
- [ ] `src/Hermod.Data/Entities/PlayPostEntity.cs` exists
- [ ] `src/Hermod.Data/Configurations/PlayPostConfiguration.cs` exists
- [ ] `PlayEntity` has `List<PlayPostEntity> Posts` nav property
- [ ] `HermodContext` has `DbSet<PlayPostEntity> PlayPosts`
- [ ] Migration `*_AddPlayPosts.cs` exists with the unique index
- [ ] `dotnet build Hermod.slnx` succeeds
