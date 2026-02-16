namespace Hermod.BGStats.Models;

public sealed record Player
{
    public required Guid Uuid { get; init; }
    public required string Name { get; init; }
    public bool IsAnonymous { get; init; }
    public DateTime ModificationDate { get; init; }
}
