using System.Security.Claims;
using Hermod.Api.Auth;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Microsoft.AspNetCore.HttpOverrides;
using Wolverine.Http;
using Wolverine.Nats;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

var hermodConnectionString = builder.Configuration.GetConnectionString("hermod-db")
    ?? throw new InvalidOperationException("ConnectionStrings:hermod-db is not configured.");

var natsUrl = builder.Configuration.GetConnectionString("nats")
    ?? throw new InvalidOperationException("ConnectionStrings:nats is not configured.");

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
        if (discordId is null) return;

        var displayName = context.Identity?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var avatarUrl = context.User.GetProperty("avatar").GetString() is { } avatar
            ? $"https://cdn.discordapp.com/avatars/{discordId}/{avatar}.png"
            : null;

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
    opts.Durability.NodeAssignmentHealthCheckTracingEnabled = false;
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

    opts.UseNats(natsUrl)
        .AutoProvision();

    opts.ListenToNatsSubject("hermod.api");

    opts.PublishMessage<Hermod.Messages.SharePlayToGroup>()
        .ToNatsSubject("hermod.bot");

    opts.PublishMessage<Hermod.Messages.DistributePlayFile>()
        .ToNatsSubject("hermod.bot");

    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

    opts.OnException<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>()
        .RetryWithCooldown(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(250));

    // Handlers can race ahead of the HTTP endpoint's transaction commit when
    // Wolverine dispatches durable local messages in-memory. A cooldown retry
    // gives the transaction time to commit before re-querying.
    opts.OnException<InvalidOperationException>()
        .RetryWithCooldown(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2));
});

builder.Services.AddWolverineHttp();


var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    ForwardLimit = null, // Allow multiple proxy hops (e.g. tunnel → YARP → Vite → API)
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// When behind a dev tunnel, the YARP container overwrites X-Forwarded-Host
// (Set mode, not Append), so the tunnel's original host is lost. Override the
// request scheme/host explicitly from the injected tunnel URL.
if (app.Configuration["Auth:ExternalBaseUrl"] is { Length: > 0 } externalBaseUrl)
{
    var externalUri = new Uri(externalBaseUrl.TrimEnd('/'));
    app.Use((context, next) =>
    {
        context.Request.Scheme = externalUri.Scheme;
        context.Request.Host = externalUri.IsDefaultPort
            ? new HostString(externalUri.Host)
            : new HostString(externalUri.Host, externalUri.Port);
        return next();
    });
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapWolverineEndpoints(opts =>
    opts.ConfigureEndpoints(e => e.DisableAntiforgery()));

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
