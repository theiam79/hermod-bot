# Task 02 — Remove Auth

## Why
`ApiKeyAuthenticationHandler` was built for an external Bot process to authenticate against the API.
With the Bot colocated in `Hermod.Api`, there are no external clients and no authentication boundary.
Auth will be re-added in Phase 4 (Discord OAuth for the web frontend).

## Steps

### 1. Delete the auth handler
```
src/Hermod.Api/Auth/ApiKeyAuthenticationHandler.cs   (delete file)
src/Hermod.Api/Auth/                                 (delete directory if now empty)
```

### 2. Update `src/Hermod.Api/Program.cs`
Remove:
```csharp
using Hermod.Api.Auth;
```
Remove:
```csharp
builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.AuthenticationScheme, null);
builder.Services.AddAuthorization();
```
Remove:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```
Also remove the unused `using Microsoft.AspNetCore.Authentication;` if it becomes orphaned.

### 3. Update `src/Hermod.AppHost/AppHost.cs`
Remove the `apiKey` parameter and `WithEnvironment` call. Result should be:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Hermod_Api>("hermod-api");

builder.Build().Run();
```

## Acceptance Criteria
- `src/Hermod.Api/Auth/` directory no longer exists
- `Program.cs` contains no auth registration or middleware
- `AppHost.cs` contains no `apiKey` parameter
- All `[Authorize]` attributes on endpoints are also removed (handled in Task 03)
