using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Api.Endpoints.Plays;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Http;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

var hermodConnectionString = builder.Configuration.GetConnectionString("hermod-db")
    ?? throw new InvalidOperationException("ConnectionStrings:hermod-db is not configured.");

builder.Services.AddDbContextWithWolverineIntegration<HermodContext>(options =>
    options.UseNpgsql(hermodConnectionString));

builder.EnrichNpgsqlDbContext<HermodContext>(settings =>
{
    // Wolverine manages retries at the handler level
    settings.DisableRetry = true;
});

builder.AddNpgsqlDbContext<AuthDbContext>("auth-db");

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Discord";
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
})
.AddDiscord(options =>
{
    options.ClientId = builder.Configuration["Discord:ClientId"]
        ?? throw new InvalidOperationException("Discord:ClientId is not configured.");
    options.ClientSecret = builder.Configuration["Discord:ClientSecret"]
        ?? throw new InvalidOperationException("Discord:ClientSecret is not configured.");
    options.CallbackPath = "/signin-discord";
    options.SaveTokens = false;
    options.Events.OnCreatingTicket = async context =>
    {
        var discordId = context.Identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var displayName = context.Identity?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var avatarUrl = context.User.GetProperty("avatar").GetString() is { } avatar
            ? $"https://cdn.discordapp.com/avatars/{discordId}/{avatar}.png"
            : null;

        if (discordId is null) return;

        var authDb = context.HttpContext.RequestServices.GetRequiredService<AuthDbContext>();
        var hermodDb = context.HttpContext.RequestServices.GetRequiredService<HermodContext>();

        var login = await authDb.ExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == "Discord" && e.ProviderKey == discordId);

        Guid userId;
        if (login is null)
        {
            // First login — create auth user + external login + app profile
            userId = Guid.NewGuid();

            authDb.Users.Add(new AuthUser
            {
                Id = userId,
                DisplayName = displayName,
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            });

            authDb.ExternalLogins.Add(new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "Discord",
                ProviderKey = discordId,
                CreatedAt = DateTime.UtcNow,
            });

            hermodDb.UserProfiles.Add(new UserProfileEntity
            {
                Id = UserId.From(userId),
                DisplayName = displayName,
            });

            await authDb.SaveChangesAsync();
            await hermodDb.SaveChangesAsync();
        }
        else
        {
            // Returning user — update last login + sync display name
            userId = login.UserId;
            login.User.LastLoginAt = DateTime.UtcNow;
            login.User.DisplayName = displayName;
            if (avatarUrl is not null)
                login.User.AvatarUrl = avatarUrl;

            await authDb.SaveChangesAsync();
        }

        context.Identity!.AddClaim(new Claim("hermod:user_id", userId.ToString()));
    };
});

builder.Services.AddAuthorization();

builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithPostgresql(hermodConnectionString);
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
});

builder.Services.AddWolverineHttp();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapWolverineEndpoints();
app.MapUploadEndpoint();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var hermodContext = scope.ServiceProvider.GetRequiredService<HermodContext>();
    await hermodContext.Database.MigrateAsync();

    var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await authContext.Database.MigrateAsync();
}

app.Run();

// Make Program accessible for WebApplicationFactory in tests
public partial class Program;
