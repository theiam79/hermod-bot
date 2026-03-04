namespace Hermod.BGStats.Models;

public sealed record Location
{
    public required Guid Uuid { get; init; }
    public required string Name { get; init; }
    public DateTime ModificationDate { get; init; }
}
