# Task 01 — Remove Hermod.Contracts Project

## Why
`Hermod.Contracts` was created for a separate Bot process to consume shared DTOs over HTTP.
With the Bot colocated in `Hermod.Api`, there is no external client and no need for this project.
It can be re-added later if we introduce a separate client.

## Steps

### 1. Delete the project directory
```
src/Hermod.Contracts/   (delete entire directory)
```

### 2. Remove from `Hermod.slnx`
Remove the line:
```xml
<Project Path="src/Hermod.Contracts/Hermod.Contracts.csproj" />
```

### 3. Remove from `src/Hermod.Api/Hermod.Api.csproj`
Remove:
```xml
<PackageReference Include="Riok.Mapperly" Version="4.*" PrivateAssets="all" />
```
and:
```xml
<ProjectReference Include="..\Hermod.Contracts\Hermod.Contracts.csproj" />
```

Note: Mapperly is only needed for the `ResponseMapper` which maps entities to Contracts DTOs.
Once both are gone, Mapperly has no purpose.

## Acceptance Criteria
- `src/Hermod.Contracts/` directory no longer exists
- `Hermod.slnx` contains no reference to `Hermod.Contracts`
- `Hermod.Api.csproj` contains no reference to `Hermod.Contracts` or `Riok.Mapperly`
- No compile errors from removal (other tasks handle removing the files that use Contracts types)
