using System.Text.Json.Serialization;

namespace Hermod.BGStats.Models;

public sealed record PlayLink
{
    public string Board { get; init; } = "";
    public int DurationMin { get; init; }
    public GameElement Game { get; init; } = new();
    public DateTime PlayDate { get; init; }
    public List<PlayerElement> Players { get; init; } = [];
    public string SourceName { get; init; } = "Hermod.Bot";
    public string SourcePlayId { get; init; } = "";

    public sealed record PlayerElement
    {
        public bool StartPlayer { get; init; }
        public string Name { get; init; } = "";
        public int Rank { get; init; }
        public string Role { get; init; } = "";
        public double Score { get; init; }
        public string SourcePlayerId { get; init; } = "";
        public bool Winner { get; init; }
    }

    public sealed record GameElement
    {
        public int BggId { get; init; }
        public bool HighestWins { get; init; }
        public string Name { get; init; } = "";
        public bool NoPoints { get; init; }
        public string SourceGameId { get; init; } = "";
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PlayLink))]
internal partial class PlayLinkJsonContext : JsonSerializerContext;
