using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using NCalc;

namespace Hermod.BGstats
{
    public class Play
    {
        public Guid Uuid { get; init; }
        public DateTime ModificiationDate { get; init; }
        public DateTime EnteredDate { get; init; }
        public DateTime DatePlayed { get; init; }
        public bool UsesTeams { get; init; }
        public TimeSpan Duration { get; init; }
        public bool IgnoredForStatistics { get; init; }
        public bool ManualWinner { get; init; }
        public int Rounds { get; init; }
        public string Board { get; init; } = "";
        public int ScoringSettings { get; init; }
        public Game Game { get; init; } = new();
        public Location Location { get; init; } = new();
        public List<Score> Scores { get; init; } = new();
        //public string ShareLink => CreateShareLink();
        public string CreateShareLink()
        {
            var playLink = new PlayLink
            {
                Board = Board,
                DurationMin = Duration.Minutes,
                PlayDate = DatePlayed,
                SourcePlayId = Uuid.ToString(),
                Game = new PlayLink.GameElement
                {
                    BggId = Game.BggId,
                    HighestWins = Game.HighestScoreWins,
                    Name = Game.Name,
                    NoPoints = Game.NoPoints,
                    SourceGameId = Game.Uuid.ToString()
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
                        Role = s.Role
                    })
                    .ToList()
            };

            var serialized = JsonSerializer.Serialize(playLink, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var encoded = HttpUtility.UrlEncode(serialized);
            var httpString = "https://app.bgstatsapp.com/createPlay.html?data=";
            var bgString = "bgstats://app.bgstatsapp.com/createPlay.html?data=";

            return $"{httpString}{encoded}";
        }
    }

    public class Location
    {
        public Guid Uuid { get; init; }
        public string Name { get; init; } = "";
        public DateTime ModificationDate { get; init; }
    }

    public class Game : Item
    {
        public List<Expansion> Expansions { get; init; } = new();
    }

    public class Expansion : Item { }

    public class Item
    {
        public Guid Uuid { get; init; }
        public string Name { get; init; } = "";
        public DateTime ModificationDate { get; init; }
        public bool Cooperative { get; init; }
        public bool HighestScoreWins { get; init; }
        public bool NoPoints { get; init; }
        public bool UsesTeams { get; init; }
        public string ThumbnailUrl { get; init; } = "";
        public string ImageUrl { get; init; } = "";
        public string BggName { get; init; } = "";
        public int BggYear { get; init; }
        public int BggId { get; init; }
        public string Designers { get; init; } = "";
        public bool IsBaseGame { get; init; }
        public bool IsExpansion { get; init; }
        public int Rating { get; init; }
        public int MinPlayerCount { get; init; }
        public int MaxPlayerCount { get; init; }
        public TimeSpan MinPlayTime { get; init; }
        public TimeSpan MaxPlayTime { get; init; }
        public int MinAge { get; init; }
    }

    public class Player
    {
        public bool IsAnonymous { get; init; }
        public DateTime ModificationDate { get; init; }
        public string Name { get; init; } = "";
        public Guid Uuid { get; init; }
        public string BggUsername { get; init; } = "";
    }

    public class Score
    {
        public Player Player { get; init; } = new();
        public string? ScoreExpression { get; init; }
        public bool Winner { get; init; }
        public bool NewPlayer { get; init; }
        public bool StartPlayer { get; init; }
        public string Role { get; init; } = "";
        public int Rank { get; init; }
        public int SeatOrder { get; init; }
        public string? Team { get; init; }
        public string StartPosition { get; init; } = "";
        public double? CalculateScore()
        {
            if (!calculated)
            {
                Calculate();
            }
            return calculatedValue;
        }

        private bool calculated;
        private double? calculatedValue;
        private void Calculate()
        {
            calculatedValue = ScoreExpression switch
            {
                null => default,
                "" => default,
                not null when double.TryParse(ScoreExpression, out var parsed) => parsed,
                not null when TryEvaluate(ScoreExpression, out var evaluated) => evaluated,
                _ => default
            };

            calculated = true;
        }

        private bool TryEvaluate(string expression, out double? result)
        {
            result = default;
            try
            {
                var exp = new Expression(ScoreExpression, EvaluateOptions.None);
                result = exp.Evaluate() as double?;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
