using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;

namespace Hermod.Api.Handlers;

public static class RegisterGuildHandler
{
    public static async Task<GuildRegistered> Handle(RegisterGuild message, HermodContext db)
    {
        var groupId = GroupId.From(GroupIdFactory.ForDiscordGuild(message.DiscordGuildId));

        var group = await db.Groups.FindAsync(groupId);
        if (group is not null)
        {
            group.Name = message.GuildName;
        }
        else
        {
            group = new GroupEntity
            {
                Id = groupId,
                Name = message.GuildName,
                AllowSharing = false,
            };
            db.Groups.Add(group);
        }

        return new GuildRegistered(group.Id.Value, message.DiscordGuildId);
    }
}
