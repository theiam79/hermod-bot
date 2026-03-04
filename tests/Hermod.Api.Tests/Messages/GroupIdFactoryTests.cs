using Hermod.Messages;
using TUnit.Core;

namespace Hermod.Api.Tests.Messages;

public class GroupIdFactoryTests
{
    [Test]
    public async Task ForCommunity_Discord_MatchesForDiscordGuild()
    {
        var guildId = 123456789UL;

        var fromCommunity = GroupIdFactory.ForCommunity("Discord", guildId.ToString());
        var fromGuild = GroupIdFactory.ForDiscordGuild(guildId);

        await Assert.That(fromCommunity).IsEqualTo(fromGuild);
    }

    [Test]
    public async Task ForCommunity_Deterministic()
    {
        var first = GroupIdFactory.ForCommunity("Discord", "987654321");
        var second = GroupIdFactory.ForCommunity("Discord", "987654321");

        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task ForCommunity_DifferentIds_DifferentOutputs()
    {
        var a = GroupIdFactory.ForCommunity("Discord", "111");
        var b = GroupIdFactory.ForCommunity("Discord", "222");

        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task ForCommunity_UnknownProvider_Throws()
    {
        await Assert.That(() => GroupIdFactory.ForCommunity("UnknownProvider", "123"))
            .ThrowsException()
            .WithMessageContaining("Unknown provider");
    }
}
