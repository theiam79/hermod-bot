namespace Hermod.Auth;

public record ExternalUserInfo(Guid UserId, string DisplayName);

public interface IExternalUserResolver
{
    Task<ExternalUserInfo?> ResolveAsync(string provider, string providerKey, CancellationToken ct = default);
}
