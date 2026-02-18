# Task 01 — UploadEntity Schema + Messages

## Why
We need to persist the raw `.bgsplay` file bytes so the distribution handler can attach them
to Discord DMs. An `UploadEntity` groups all plays from a single file and stores the blob.
New message types decouple play extraction from file distribution.

## Steps

### 1. Add `UploadId` to `src/Hermod.Data/Ids.cs`

```csharp
[ValueObject<Guid>]
public readonly partial struct UploadId;
```

### 2. Create `src/Hermod.Data/Entities/UploadEntity.cs`

```csharp
namespace Hermod.Data.Entities;

public class UploadEntity
{
    public UploadId Id { get; set; }
    public UserId? UploadedById { get; set; }
    public required byte[] FileBytes { get; set; }
    public required string FileName { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity? UploadedBy { get; set; }
    public List<PlayEntity> Plays { get; set; } = [];
}
```

### 3. Create `src/Hermod.Data/Configurations/UploadConfiguration.cs`

```csharp
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hermod.Data.Configurations;

public class UploadConfiguration : IEntityTypeConfiguration<UploadEntity>
{
    public void Configure(EntityTypeBuilder<UploadEntity> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, v => UploadId.From(v));

        builder.Property(u => u.UploadedById)
            .HasConversion(id => id!.Value.Value, v => UserId.From(v));

        builder.Property(u => u.FileBytes).IsRequired();
        builder.Property(u => u.FileName).IsRequired().HasMaxLength(255);

        builder.HasOne(u => u.UploadedBy)
            .WithMany()
            .HasForeignKey(u => u.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

### 4. Add `UploadId` FK to `src/Hermod.Data/Entities/PlayEntity.cs`

```csharp
public UploadId? UploadId { get; set; }
public UploadEntity? Upload { get; set; }
```

### 5. Add FK config to `PlayConfiguration`

Add UploadId conversion + FK to the existing `PlayConfiguration`:

```csharp
builder.Property(p => p.UploadId)
    .HasConversion(
        id => id.HasValue ? id.Value.Value : (Guid?)null,
        v => v.HasValue ? UploadId.From(v.Value) : null);

builder.HasOne(p => p.Upload)
    .WithMany(u => u.Plays)
    .HasForeignKey(p => p.UploadId)
    .OnDelete(DeleteBehavior.SetNull);
```

### 6. Add `DbSet` to `src/Hermod.Data/HermodContext.cs`

```csharp
public DbSet<UploadEntity> Uploads => Set<UploadEntity>();
```

### 7. Add EF migration

```bash
dotnet ef migrations add AddUploads \
    --project src/Hermod.Data \
    --startup-project src/Hermod.Api
```

### 8. Create new messages

**`src/Hermod.Api/Messages/PlayFileUploaded.cs`**
```csharp
namespace Hermod.Api.Messages;

public record PlayFileUploaded(Guid UploadId, Guid? GroupId, string? SenderDiscordId, Guid? MePlayerUuid);
```

**`src/Hermod.Api/Messages/DistributePlayFile.cs`**
```csharp
namespace Hermod.Api.Messages;

public record DistributePlayFile(Guid UploadId, string PlayerUuid);
```

### 9. Add `UploadId` to `PlayExtracted`

```csharp
public record PlayExtracted(Play ParsedPlay, Guid? GroupId, string? SenderDiscordId, Guid? MePlayerUuid, Guid? UploadId);
```

## Notes
- `UploadedById` is nullable to support API uploads without auth context.
- `OnDelete(SetNull)` on both FKs — deleting an upload doesn't cascade-delete plays,
  and deleting a user doesn't cascade-delete uploads.
- Existing `PlayExtracted` call sites (MessageReceivedHandler, UploadPlays) will break
  after adding the `UploadId` parameter. They are fixed in task-02.

## Acceptance Criteria
- [ ] `UploadId` exists in `Ids.cs`
- [ ] `UploadEntity` exists with FileBytes, FileName, UploadedById, CreatedAt
- [ ] `UploadConfiguration` exists with proper conversions and FK
- [ ] `PlayEntity` has `UploadId?` FK and `Upload` nav property
- [ ] `PlayConfiguration` has UploadId conversion and FK config
- [ ] `HermodContext` has `DbSet<UploadEntity> Uploads`
- [ ] Migration `*_AddUploads.cs` exists
- [ ] `PlayFileUploaded` and `DistributePlayFile` message records exist
- [ ] `PlayExtracted` has `UploadId` parameter
- [ ] `dotnet build Hermod.slnx` succeeds (call sites updated temporarily or in task-02)
