using System.Security.Cryptography;
using System.Text;

namespace Hermod.Messages;

public static class GroupIdFactory
{
    // Stable namespace GUIDs — one per provider. Never change these.
    private static readonly Dictionary<string, Guid> ProviderNamespaces = new()
    {
        ["Discord"] = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
    };

    public static Guid ForCommunity(string provider, string platformId)
    {
        if (!ProviderNamespaces.TryGetValue(provider, out var namespaceId))
            throw new ArgumentException($"Unknown provider: {provider}", nameof(provider));

        return CreateVersion5(namespaceId, Encoding.UTF8.GetBytes(platformId));
    }

    public static Guid ForDiscordGuild(ulong discordGuildId) =>
        ForCommunity("Discord", discordGuildId.ToString());

    /// <summary>
    /// UUID v5 per RFC 4122: SHA-1 hash of namespace + name, with version/variant bits set.
    /// </summary>
    private static Guid CreateVersion5(Guid namespaceId, byte[] name)
    {
        // Namespace GUID must be in big-endian (network byte order) for hashing
        var namespaceBytes = namespaceId.ToByteArray();
        SwapGuidBytesToBigEndian(namespaceBytes);

        var hash = SHA1.HashData([.. namespaceBytes, .. name]);

        // Take first 16 bytes of the hash
        var guidBytes = hash[..16];

        // Set version to 5 (bits 4-7 of byte 6)
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);

        // Set variant to RFC 4122 (bits 6-7 of byte 8)
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        // Swap back to little-endian for .NET Guid constructor
        SwapGuidBytesToBigEndian(guidBytes);

        return new Guid(guidBytes);
    }

    /// <summary>
    /// .NET Guid.ToByteArray() uses mixed-endian (little-endian for first 3 groups, big-endian for last 2).
    /// RFC 4122 requires big-endian throughout. This swaps the first 3 groups.
    /// </summary>
    private static void SwapGuidBytesToBigEndian(byte[] bytes)
    {
        // Swap bytes 0-3 (uint32)
        (bytes[0], bytes[3]) = (bytes[3], bytes[0]);
        (bytes[1], bytes[2]) = (bytes[2], bytes[1]);
        // Swap bytes 4-5 (uint16)
        (bytes[4], bytes[5]) = (bytes[5], bytes[4]);
        // Swap bytes 6-7 (uint16)
        (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
    }
}
