using Microsoft.EntityFrameworkCore;

namespace Hermod.Auth;

public class ExternalUserResolver(AuthDbContext authDb) : IExternalUserResolver
{
    public async Task<ExternalUserInfo?> ResolveAsync(string provider, string providerKey, CancellationToken ct = default)
    {
        var login = await authDb.ExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderKey == providerKey, ct);

        if (login is null)
            return null;

        return new ExternalUserInfo(login.UserId, login.User.DisplayName);
    }
}
