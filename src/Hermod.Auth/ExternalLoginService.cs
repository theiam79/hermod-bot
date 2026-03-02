using Microsoft.EntityFrameworkCore;

namespace Hermod.Auth;

public class ExternalLoginService(AuthDbContext authDb, TimeProvider timeProvider)
{
    public async Task<(Guid UserId, bool IsNew)> ProvisionOrUpdateAsync(
        string provider, string providerKey, string displayName, string? avatarUrl)
    {
        var login = await authDb.ExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderKey == providerKey);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (login is null)
        {
            var userId = Guid.NewGuid();

            authDb.Users.Add(new AuthUser
            {
                Id = userId,
                DisplayName = displayName,
                AvatarUrl = avatarUrl,
                CreatedAt = now,
                LastLoginAt = now,
            });

            authDb.ExternalLogins.Add(new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = provider,
                ProviderKey = providerKey,
                CreatedAt = now,
            });

            await authDb.SaveChangesAsync();
            return (userId, true);
        }

        login.User.LastLoginAt = now;
        login.User.DisplayName = displayName;
        if (avatarUrl is not null)
            login.User.AvatarUrl = avatarUrl;

        await authDb.SaveChangesAsync();
        return (login.UserId, false);
    }
}
