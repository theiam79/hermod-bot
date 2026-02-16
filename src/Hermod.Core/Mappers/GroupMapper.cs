using Hermod.Core.Models;
using Hermod.Data.Entities;

namespace Hermod.Core.Mappers;

public static class GroupMapper
{
    public static GroupSummary ToSummary(GroupEntity entity)
    {
        return new GroupSummary
        {
            Id = entity.Id,
            Name = entity.Name,
            DiscordGuildId = entity.DiscordGuildId,
            DiscordPostChannelId = entity.DiscordPostChannelId,
            AllowSharing = entity.AllowSharing,
        };
    }
}
