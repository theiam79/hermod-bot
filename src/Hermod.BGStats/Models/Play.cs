using System.Text.Json;
using System.Web;

namespace Hermod.BGStats.Models;

public sealed record Play
{
    public required Guid Uuid { get; init; }
    public DateTime ModificationDate { get; init; }
    public DateTime EnteredDate { get; init; }
    public DateTime DatePlayed { get; init; }
    public bool UsesTeams { get; init; }
    public TimeSpan Duration { get; init; }
    public bool IgnoredForStatistics { get; init; }
    public bool ManualWinner { get; init; }
    public int Rounds { get; init; }
    public string Board { get; init; } = "";
    public string? Comments { get; init; }
    public int ScoringSettings { get; init; }
    public required Game Game { get; init; }
    public required Location Location { get; init; }
    public List<Score> Scores { get; init; } = [];
    public List<Game> ExpansionsUsed { get; init; } = [];
    public Scoresheet? Scoresheet { get; init; }

    public string CreateShareLink()
    {
        var playLink = new PlayLink
        {
            Board = Board,
            DurationMin = (int)Duration.TotalMinutes,
            PlayDate = DatePlayed,
            SourcePlayId = Uuid.ToString(),
            Game = new PlayLink.GameElement
            {
                BggId = Game.BggId,
                HighestWins = Game.HighestScoreWins,
                Name = Game.Name,
                NoPoints = Game.NoPoints,
                SourceGameId = Game.Uuid.ToString(),
            },
            Players = Scores
                .Select(s => new PlayLink.PlayerElement
                {
                    Name = s.Player.Name,
                    SourcePlayerId = s.Player.Uuid.ToString(),
                    StartPlayer = s.StartPlayer,
                    Winner = s.Winner,
                    Score = 0,
                    Rank = s.Rank,
                    Role = s.Role,
                })
                .ToList(),
        };

        var serialized = JsonSerializer.Serialize(playLink, PlayLinkJsonContext.Default.PlayLink);
        var encoded = HttpUtility.UrlEncode(serialized);
        return $"https://app.bgstatsapp.com/createPlay.html?data={encoded}";
    }
}
