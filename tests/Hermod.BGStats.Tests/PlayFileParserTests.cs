using Hermod.BGStats;

namespace Hermod.BGStats.Tests;

public class PlayFileParserTests
{
    private static string GetTestDataPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
    }

    [Test]
    public async Task Parse_SinglePlayFile_ReturnsOnePlay()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("GuardsofAtlantisII-play251101205029.bgsplay"));

        var result = PlayFileParser.Parse(json);

        await Assert.That(result.Plays).HasCount().EqualTo(1);
    }

    [Test]
    public async Task Parse_MultiPlayFile_ReturnsAllPlays()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay"));

        var result = PlayFileParser.Parse(json);

        await Assert.That(result.Plays).HasCount().EqualTo(6);
    }

    [Test]
    public async Task Parse_SinglePlayFile_MapsGameCorrectly()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("GuardsofAtlantisII-play251101205029.bgsplay"));

        var result = PlayFileParser.Parse(json);
        var play = result.Plays[0];

        await Assert.That(play.Game.Name).IsEqualTo("Guards of Atlantis II");
        await Assert.That(play.Game.BggId).IsEqualTo(267609);
        await Assert.That(play.Game.Designers).IsEqualTo("Artyom Nichipurov");
    }

    [Test]
    public async Task Parse_SinglePlayFile_MapsPlayersCorrectly()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("GuardsofAtlantisII-play251101205029.bgsplay"));

        var result = PlayFileParser.Parse(json);
        var play = result.Plays[0];

        await Assert.That(play.Scores).HasCount().EqualTo(6);
        await Assert.That(play.Scores.Select(s => s.Player.Name))
            .Contains("Tyler Hundley");
    }

    [Test]
    public async Task Parse_TeamPlay_MapsTeamsAndRoles()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("GuardsofAtlantisII-play251101205029.bgsplay"));

        var result = PlayFileParser.Parse(json);
        var play = result.Plays[0];

        await Assert.That(play.UsesTeams).IsTrue();

        var tyler = play.Scores.First(s => s.Player.Name == "Tyler Hundley");
        await Assert.That(tyler.Team).IsEqualTo("0");
        await Assert.That(tyler.Winner).IsTrue();
        await Assert.That(tyler.Role).IsEqualTo("Arien the Tidemaster／Tigerclaw the Cutpurse");
    }

    [Test]
    public async Task Parse_PlayWithScores_CalculatesCorrectly()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay"));

        var result = PlayFileParser.Parse(json);

        // First play is John Company with numeric scores
        var johnCompany = result.Plays[0];
        await Assert.That(johnCompany.Game.Name).IsEqualTo("John Company: Second Edition");

        var winner = johnCompany.Scores.First(s => s.Winner);
        await Assert.That(winner.CalculateScore()).IsEqualTo(11);
    }

    [Test]
    public async Task Parse_PlayWithLocation_MapsLocationCorrectly()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay"));

        var result = PlayFileParser.Parse(json);
        var play = result.Plays[0];

        await Assert.That(play.Location.Name).IsEqualTo("Robert's");
    }

    [Test]
    public async Task Parse_PlayWithDuration_MapsDurationCorrectly()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("GuardsofAtlantisII-play251101205029.bgsplay"));

        var result = PlayFileParser.Parse(json);
        var play = result.Plays[0];

        await Assert.That(play.Duration).IsEqualTo(TimeSpan.FromMinutes(75));
    }

    [Test]
    public async Task Parse_MeRefId_IsPreserved()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("GuardsofAtlantisII-play251101205029.bgsplay"));

        var result = PlayFileParser.Parse(json);

        await Assert.That(result.MeRefId).IsEqualTo(1);
    }

    [Test]
    public async Task Parse_PlayWithScoresheet_ParsesScoresheetData()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay"));

        var result = PlayFileParser.Parse(json);

        // SETI play (index 1) has a scoresheet
        var seti = result.Plays[1];
        await Assert.That(seti.Scoresheet).IsNotNull();
        await Assert.That(seti.Scoresheet!.BggId).IsEqualTo(418059);
        await Assert.That(seti.Scoresheet.Groups).HasCount().GreaterThan(0);

        var firstGroup = seti.Scoresheet.Groups[0];
        await Assert.That(firstGroup.Rows).HasCount().GreaterThan(0);
        await Assert.That(firstGroup.Rows[0].Label).IsEqualTo("Score track");
    }

    [Test]
    public async Task Parse_PlayWithExpansions_MapsExpansionsUsed()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay"));

        var result = PlayFileParser.Parse(json);

        // Elder Scrolls play (index 4) uses an expansion
        var elderScrolls = result.Plays[4];
        await Assert.That(elderScrolls.ExpansionsUsed).HasCount().EqualTo(1);
        await Assert.That(elderScrolls.ExpansionsUsed[0].Name)
            .Contains("Valenwood");
    }

    [Test]
    public async Task Parse_CooperativeGame_MapsCorrectly()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay"));

        var result = PlayFileParser.Parse(json);

        // The Crew (index 5) is cooperative
        var theCrew = result.Plays[5];
        await Assert.That(theCrew.Game.Cooperative).IsTrue();
        await Assert.That(theCrew.Scores.All(s => s.Winner)).IsTrue();
    }

    [Test]
    public async Task Parse_PlayWithComments_MapsComments()
    {
        var json = await File.ReadAllTextAsync(
            GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay"));

        var result = PlayFileParser.Parse(json);

        // The Crew play has comments
        var theCrew = result.Plays[5];
        await Assert.That(theCrew.Comments).IsEqualTo("Mission #1");
    }

    [Test]
    public async Task ParseAsync_Stream_ProducesSameResultAsString()
    {
        var path = GetTestDataPath("PlayedGames-play260216015200 (1).bgsplay");
        var json = await File.ReadAllTextAsync(path);
        var stringResult = PlayFileParser.Parse(json);

        await using var stream = File.OpenRead(path);
        var streamResult = await PlayFileParser.ParseAsync(stream);

        await Assert.That(streamResult.Plays).HasCount().EqualTo(stringResult.Plays.Count);
        await Assert.That(streamResult.MeRefId).IsEqualTo(stringResult.MeRefId);
    }
}
