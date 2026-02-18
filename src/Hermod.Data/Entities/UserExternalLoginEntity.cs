namespace Hermod.Data.Entities;

public class UserExternalLoginEntity
{
    public ExternalLoginId Id { get; set; }
    public UserId UserId { get; set; }

    /// <summary>Provider name, e.g. "Discord", "BGG".</summary>
    public required string Provider { get; set; }

    /// <summary>Provider's unique key for the user (Discord: ulong as string; BGG: int as string).</summary>
    public required string ProviderKey { get; set; }

    public UserEntity User { get; set; } = null!;
}
