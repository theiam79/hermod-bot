using Hermod.Bot.Embeds;
using Hermod.Messages;
using NetCord;
using NetCord.Rest;
using TUnit.Core;

namespace Hermod.Bot.Tests.Embeds;

public class PlayEmbedBuilderTests
{
    private static readonly Guid TestPlayId = Guid.NewGuid();

    private static PlaySnapshot CreateSnapshot(List<PlayerSnapshot>? players = null)
    {
        players ??=
        [
            new PlayerSnapshot("uuid-1", "Alice", "10", null, true, 1, null, null, null),
            new PlayerSnapshot("uuid-2", "Bob", "8", null, false, 2, null, null, null),
        ];

        return new PlaySnapshot(
            "Catan",
            DateTime.UtcNow,
            TimeSpan.FromMinutes(90),
            "Home",
            null,
            null,
            null,
            13,
            players);
    }

    [Test]
    public async Task Build_WithDiscordIds_RendersAsMentions()
    {
        var userId = Guid.NewGuid();
        var discordId = 123456789012345678UL;

        var players = new List<PlayerSnapshot>
        {
            new("uuid-1", "Alice", "10", null, true, 1, null, null, userId),
            new("uuid-2", "Bob", "8", null, false, 2, null, null, null),
        };
        var snapshot = CreateSnapshot(players);

        var discordUserIds = new Dictionary<Guid, ulong> { [userId] = discordId };
        var embed = PlayEmbedBuilder.Build(snapshot, discordUserIds);

        var fieldValue = embed.Fields!.First().Value;
        await Assert.That(fieldValue).Contains($"<@{discordId}>");
        await Assert.That(fieldValue).Contains("Bob");
        await Assert.That(fieldValue).DoesNotContain("Alice");
    }

    [Test]
    public async Task Build_WithoutDiscordId_RendersPlainName()
    {
        var players = new List<PlayerSnapshot>
        {
            new("uuid-1", "Alice", "10", null, true, 1, null, null, Guid.NewGuid()),
            new("uuid-2", "Bob", "8", null, false, 2, null, null, null),
        };
        var snapshot = CreateSnapshot(players);

        // Dictionary does not contain the player's MappedUserId
        var discordUserIds = new Dictionary<Guid, ulong>();
        var embed = PlayEmbedBuilder.Build(snapshot, discordUserIds);

        var fieldValue = embed.Fields!.First().Value;
        await Assert.That(fieldValue).Contains("Alice");
        await Assert.That(fieldValue).Contains("Bob");
        await Assert.That(fieldValue).DoesNotContain("<@");
    }

    [Test]
    public async Task Build_NullDictionary_RendersAllPlainNames()
    {
        var players = new List<PlayerSnapshot>
        {
            new("uuid-1", "Alice", "10", null, true, 1, null, null, Guid.NewGuid()),
            new("uuid-2", "Bob", "8", null, false, 2, null, null, Guid.NewGuid()),
        };
        var snapshot = CreateSnapshot(players);

        var embed = PlayEmbedBuilder.Build(snapshot, null);

        var fieldValue = embed.Fields!.First().Value;
        await Assert.That(fieldValue).Contains("Alice");
        await Assert.That(fieldValue).Contains("Bob");
        await Assert.That(fieldValue).DoesNotContain("<@");
    }

    [Test]
    public async Task Build_TeamLayout_WithDiscordIds_RendersAsMentions()
    {
        var userId = Guid.NewGuid();
        var discordId = 987654321098765432UL;

        var players = new List<PlayerSnapshot>
        {
            new("uuid-1", "Alice", "10", null, true, 1, null, "Red", userId),
            new("uuid-2", "Bob", "8", null, false, 2, null, "Blue", null),
        };
        var snapshot = CreateSnapshot(players);

        var discordUserIds = new Dictionary<Guid, ulong> { [userId] = discordId };
        var embed = PlayEmbedBuilder.Build(snapshot, discordUserIds);

        var allFieldValues = string.Join("\n", embed.Fields!.Select(f => f.Value));
        await Assert.That(allFieldValues).Contains($"<@{discordId}>");
        await Assert.That(allFieldValues).Contains("Bob");
        await Assert.That(allFieldValues).DoesNotContain("Alice");
    }

    [Test]
    public async Task BuildClaimButton_WithUnclaimedPlayers_ReturnsButton()
    {
        var snapshot = CreateSnapshot([
            new PlayerSnapshot("uuid-1", "Alice", "10", null, true, 1, null, null, null),
            new PlayerSnapshot("uuid-2", "Bob", "8", null, false, 2, null, null, Guid.NewGuid()),
        ]);

        var row = PlayEmbedBuilder.BuildClaimButton(snapshot, TestPlayId);

        await Assert.That(row).IsNotNull();
        var button = row!.Components.OfType<ButtonProperties>().Single();
        await Assert.That(button.CustomId).IsEqualTo($"claim-player-btn:{TestPlayId}");
        await Assert.That(button.Style).IsEqualTo(ButtonStyle.Secondary);
        await Assert.That(button.Label).IsEqualTo("Claim a player");
    }

    [Test]
    public async Task BuildClaimButton_AllPlayersClaimed_ReturnsNull()
    {
        var snapshot = CreateSnapshot([
            new PlayerSnapshot("uuid-1", "Alice", "10", null, true, 1, null, null, Guid.NewGuid()),
            new PlayerSnapshot("uuid-2", "Bob", "8", null, false, 2, null, null, Guid.NewGuid()),
        ]);

        var row = PlayEmbedBuilder.BuildClaimButton(snapshot, TestPlayId);

        await Assert.That(row).IsNull();
    }
}
