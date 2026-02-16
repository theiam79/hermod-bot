using System.Text.Json;
using Hermod.BGStats.Converters;
using Hermod.BGStats.Models;

namespace Hermod.BGStats;

public static class PlayFileParser
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new DateTimeConverter(),
            new IntToBoolConverter(),
        },
    };

    private static readonly JsonSerializerOptions ScoresheetOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Parses a .bgsplay JSON string and returns all plays in the file.
    /// </summary>
    public static PlayFileResult Parse(string json)
    {
        var file = JsonSerializer.Deserialize<PlayFile>(json, DeserializeOptions)
            ?? throw new InvalidOperationException("Failed to deserialize .bgsplay file.");

        var plays = file.Plays.Select(p => MapPlay(file, p)).ToList();

        return new PlayFileResult
        {
            MeRefId = file.UserInfo.MeRefId,
            Plays = plays,
        };
    }

    /// <summary>
    /// Parses a .bgsplay file from a stream.
    /// </summary>
    public static async Task<PlayFileResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var file = await JsonSerializer.DeserializeAsync<PlayFile>(stream, DeserializeOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize .bgsplay file.");

        var plays = file.Plays.Select(p => MapPlay(file, p)).ToList();

        return new PlayFileResult
        {
            MeRefId = file.UserInfo.MeRefId,
            Plays = plays,
        };
    }

    private static Play MapPlay(PlayFile file, PlayFile.PlaySection playSection)
    {
        var location = file.Locations.FirstOrDefault(l => l.Id == playSection.LocationRefId);
        var game = file.Games.First(g => g.Id == playSection.GameRefId);

        var expansionGameRefIds = playSection.ExpansionPlays.Select(ep => ep.GameRefId).ToHashSet();
        var expansions = file.Games
            .Where(g => expansionGameRefIds.Contains(g.Id))
            .Select(MapGameSection)
            .ToList();

        Scoresheet? scoresheet = null;
        if (!string.IsNullOrEmpty(playSection.Scoresheet))
        {
            try
            {
                scoresheet = JsonSerializer.Deserialize<Scoresheet>(playSection.Scoresheet, ScoresheetOptions);
            }
            catch
            {
                // Scoresheet parsing is best-effort; don't fail the whole play
            }
        }

        return new Play
        {
            Uuid = playSection.Uuid,
            ModificationDate = playSection.ModificationDate,
            EnteredDate = playSection.EntryDate,
            DatePlayed = playSection.PlayDate,
            UsesTeams = playSection.UsesTeams,
            Duration = TimeSpan.FromMinutes(playSection.DurationMin),
            IgnoredForStatistics = playSection.Ignored,
            ManualWinner = playSection.ManualWinner,
            Rounds = playSection.Rounds,
            Board = playSection.Board,
            Comments = playSection.Comments,
            ScoringSettings = playSection.ScoringSetting,
            Location = location is not null
                ? new Location
                {
                    Uuid = location.Uuid,
                    Name = location.Name,
                    ModificationDate = location.ModificationDate,
                }
                : new Location { Uuid = Guid.Empty, Name = "" },
            Game = MapGameSection(game) with
            {
                Expansions = expansions,
            },
            Scores = playSection.PlayerScores
                .Select(ps => MapPlayerScore(file, playSection, ps))
                .ToList(),
            ExpansionsUsed = expansions,
            Scoresheet = scoresheet,
        };
    }

    private static Game MapGameSection(PlayFile.GameSection g)
    {
        return new Game
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
        };
    }

    private static Score MapPlayerScore(PlayFile file, PlayFile.PlaySection playSection, PlayFile.PlayerScoreSection ps)
    {
        var player = file.Players.First(p => p.Id == ps.PlayerRefId);

        return new Score
        {
            ScoreExpression = ps.Score,
            Winner = ps.Winner,
            NewPlayer = ps.NewPlayer,
            StartPlayer = ps.StartPlayer,
            Role = playSection.UsesTeams ? ps.TeamRole : ps.Role,
            Rank = ps.Rank,
            SeatOrder = ps.SeatOrder,
            StartPosition = ps.StartPosition,
            Team = ps.Team,
            Player = new Player
            {
                Uuid = player.Uuid,
                Name = player.Name,
                IsAnonymous = player.IsAnonymous,
                ModificationDate = player.ModificationDate,
            },
        };
    }
}

public sealed record PlayFileResult
{
    public int MeRefId { get; init; }
    public required List<Play> Plays { get; init; }
}
