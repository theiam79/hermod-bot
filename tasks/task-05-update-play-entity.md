# Task 05 — Update PlayEntity: Nullable UploadedById, Drop RawPlayFileJson

## Why
- `UploadedById` is required in the current schema but there is no user management yet.
  Making it nullable unblocks play upload without a user system.
- `RawPlayFileJson` stored the entire file JSON redundantly on every play row. Raw file
  persistence is earmarked for future review (dedicated table, document store, etc.).
  Removing it now keeps the schema clean.

## Steps

### 1. `src/Hermod.Data/Entities/PlayEntity.cs`
Change:
```csharp
public UserId UploadedById { get; set; }
// ...
public UserEntity UploadedBy { get; set; } = null!;
```
To:
```csharp
public UserId? UploadedById { get; set; }
// ...
public UserEntity? UploadedBy { get; set; }
```
Remove the property:
```csharp
public required string RawPlayFileJson { get; set; }
```

### 2. `src/Hermod.Data/Configurations/PlayConfiguration.cs`
Replace the `UploadedById` conversion and FK:
```csharp
// Old
builder.Property(p => p.UploadedById)
    .HasConversion(id => id.Value, v => UserId.From(v));
// ...
builder.HasOne(p => p.UploadedBy)
    .WithMany(u => u.UploadedPlays)
    .HasForeignKey(p => p.UploadedById);
```
With:
```csharp
// New
builder.Property(p => p.UploadedById)
    .HasConversion(id => (Guid?)id!.Value.Value, v => v.HasValue ? UserId.From(v.Value) : (UserId?)null);
// ...
builder.HasOne(p => p.UploadedBy)
    .WithMany(u => u.UploadedPlays)
    .HasForeignKey(p => p.UploadedById)
    .IsRequired(false);
```
Remove:
```csharp
builder.Property(p => p.RawPlayFileJson).IsRequired();
```

## Notes
- The nullable Vogen conversion pattern for `GroupId` already exists in this file — use that
  as the reference for how to write the nullable `UserId` conversion.
- `UserEntity.UploadedPlays` nav collection does not need to change (EF handles nullable FK side).
- This task is a **prerequisite for Task 06** (migration) and **Task 09** (handler).

## Acceptance Criteria
- `PlayEntity.UploadedById` is `UserId?`
- `PlayEntity.UploadedBy` is `UserEntity?`
- `PlayEntity.RawPlayFileJson` property does not exist
- `PlayConfiguration` has no reference to `RawPlayFileJson`
- `PlayConfiguration` FK for `UploadedById` is `IsRequired(false)`
- `dotnet build src/Hermod.Data/Hermod.Data.csproj` succeeds
