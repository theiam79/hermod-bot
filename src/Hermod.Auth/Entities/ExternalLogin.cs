namespace Hermod.Auth;

public class ExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Provider { get; set; }
    public required string ProviderKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public AuthUser User { get; set; } = null!;
}
