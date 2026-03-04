using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.BGstats
{
    /// <summary>
    /// The raw object from a .bgsplay file
    /// </summary>
    public class PlayFile
    {
        public Play MapToPlay()
        {
            var play = Plays.FirstOrDefault() ?? throw new IndexOutOfRangeException();
            var location = Locations.FirstOrDefault(l => l.Id == play.LocationRefId) ?? throw new IndexOutOfRangeException();
            var game = Games.FirstOrDefault(g => g.Id == play.GameRefId) ?? throw new IndexOutOfRangeException();

            return new Play
            {
                Uuid = play.Uuid,
                ModificiationDate = play.ModificationDate,
                EnteredDate = play.EntryDate,
                DatePlayed = play.PlayDate,
                UsesTeams = play.UsesTeams,
                Duration = TimeSpan.FromMinutes(play.DurationMin),
                IgnoredForStatistics = play.Ignored,
                ManualWinner = play.ManualWinner,
                Rounds = play.Rounds,
                Board = play.Board,
                ScoringSettings = play.ScoringSettings,
                Location = new Location
                {
                    Uuid = location.Uuid,
                    Name = location.Name,
                    ModificationDate = location.ModificationDate,
                },
                Game = new Game
                {
                    Uuid = game.Uuid,
                    Name = game.Name,
                    ModificationDate = game.ModificationDate,
                    Cooperative = game.Cooperative,
                    HighestScoreWins = game.HighestWins,
                    NoPoints = game.NoPoints,
                    UsesTeams = game.UsesTeams,
                    ThumbnailUrl = game.UrlThumb,
                    ImageUrl = game.UrlImage,
                    BggName = game.BggName,
                    BggYear = game.BggYear,
                    BggId = game.BggId,
                    Designers = game.Designers,
                    IsBaseGame = game.IsBaseGame,
                    IsExpansion = game.IsExpansion,
                    Rating = game.Rating,
                    MinPlayerCount = game.MinPlayerCount,
                    MaxPlayerCount = game.MaxPlayerCount,
                    MinPlayTime = TimeSpan.FromMinutes(game.MinPlayTime),
                    MaxPlayTime = TimeSpan.FromMinutes(game.MaxPlayTime),
                    MinAge = game.MinAge,
                    Expansions = Games
                        //.Where(g => !g.IsBaseGame)
                        .Where(g => g.Id != game.Id)
                        .Select(g => new Expansion
                        {
                            Uuid = g.Uuid,
                            Name = g.Name,
                            ModificationDate = g.ModificationDate,
                            Cooperative = g.Cooperative,
                            HighestScoreWins = g.HighestWins,
                            NoPoints = g.NoPoints,
                            UsesTeams = g.UsesTeams,
                            ThumbnailUrl = g.UrlThumb,
                            ImageUrl = g.UrlImage,
                            BggName = g.BggName,
                            BggYear = g.BggYear,
                            BggId = g.BggId,
                            Designers = g.Designers,
                            IsBaseGame = g.IsBaseGame,
                            IsExpansion = g.IsExpansion,
                            Rating = g.Rating,
                            MinPlayerCount = g.MinPlayerCount,
                            MaxPlayerCount = g.MaxPlayerCount,
                            MinPlayTime = TimeSpan.FromMinutes(g.MinPlayTime),
                            MaxPlayTime = TimeSpan.FromMinutes(g.MaxPlayTime),
                            MinAge = g.MinAge,
                        })
                        .ToList(),
                },
                Scores = play
                    .PlayerScores
                    .Select(ps => new Score
                    {
                        ScoreExpression = ps.Score,
                        Winner = ps.Winner,
                        NewPlayer = ps.NewPlayer,
                        StartPlayer = ps.StartPlayer,
                        Role = play.UsesTeams ? ps.TeamRole : ps.Role,
                        Rank = ps.Rank,
                        Team = ps.Team,
                        SeatOrder = ps.SeatOrder,
                        StartPosition = ps.StartPosition,
                        Player = Players
                            .Where(p => p.Id == ps.PlayerRefId)
                            .Select(p => new Player
                            {
                                IsAnonymous = p.IsAnonymous,
                                ModificationDate = p.ModificationDate,
                                Name = p.Name,
                                Uuid = p.Uuid,
                                BggUsername = p.BggUsername,
                            })
                            .First()
                    })
                    .ToList()
            };
        }

        public string About { get; init; } = "";
        public List<PlayerSection> Players { get; init; } = new();
        public List<LocationSection> Locations { get; init; } = new();
        public List<GameSection> Games { get; init; } = new();
        public List<PlaySection> Plays { get; init; } = new();
        public UserInfoSection UserInfo { get; init; } = new();

        public class ChallengeSection
        {

        }

        public class UserInfoSection
        {
            public int MeRefId { get; init; }
        }

        public class GameSection
        {
            public Guid Uuid { get; init; }
            public int Id { get; init; }
            public string Name { get; init; } = "";
            public DateTime ModificationDate { get; init; }
            public bool Cooperative { get; init; }
            public bool HighestWins { get; init; }
            public bool NoPoints { get; init; }
            public bool UsesTeams { get; init; }
            public string UrlThumb { get; init; } = "";
            public string UrlImage { get; init; } = "";
            public string BggName { get; init; } = "";
            public int BggYear { get; init; }
            public int BggId { get; init; }
            public string Designers { get; init; } = "";
            public bool IsBaseGame { get; init; }
            public bool IsExpansion { get; init; }
            public int Rating { get; init; }
            public int MinPlayerCount { get; init; }
            public int MaxPlayerCount { get; init; }
            public int MinPlayTime { get; init; }
            public int MaxPlayTime { get; init; }
            public int MinAge { get; init; }
        }

        public class PlaySection
        {
            public Guid Uuid { get; init; }
            public DateTime ModificationDate { get; init; }
            public DateTime EntryDate { get; init; }
            public DateTime PlayDate { get; init; }
            public bool UsesTeams { get; init; }
            public int DurationMin { get; init; }
            public bool Ignored { get; init; }
            public bool ManualWinner { get; init; }
            public int Rounds { get; init; }
            public int LocationRefId { get; init; }
            public int GameRefId { get; init; }
            public string Board { get; init; } = "";
            public int ScoringSettings { get; init; }
            public List<PlayerScoreSection> PlayerScores { get; init; } = new();
            public List<ExpansionPlaySection> ExpansionPlays { get; init; } = new();

            public class PlayerScoreSection
            {
                public string? Score { get; init; }
                public bool Winner { get; init; }
                public bool NewPlayer { get; init; }
                public bool StartPlayer { get; init; }
                public int PlayerRefId { get; init; }
                public string Role { get; init; } = "";
                public string TeamRole { get; init; } = "";
                public int Rank { get; init; }
                public int SeatOrder { get; init; }
                public string StartPosition { get; init; } = "";
                public string? Team { get; init; }
            }

            public class ExpansionPlaySection
            {
                public int GameRefId { get; init; }
            }
        }

        public class LocationSection
        {
            public Guid Uuid { get; init; }
            public int Id { get; init; }
            public string Name { get; init; } = "";
            public DateTime ModificationDate { get; init; }
        }

        public class PlayerSection
        {
            public int Id { get; init; }
            public bool IsAnonymous { get; init; }
            public DateTime ModificationDate { get; init; }
            public string Name { get; init; } = "";
            public Guid Uuid { get; init; }
            public string BggUsername { get; init; } = "";
        }
    }
}
