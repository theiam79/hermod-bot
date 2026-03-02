using Hermod.Data;
using Hermod.Data.Entities;
using Hermod.Messages;

namespace Hermod.Api.Handlers;

public static class RegisterCommunityHandler
{
    public static async Task<CommunityRegistered> Handle(RegisterCommunity message, HermodContext db)
    {
        var groupId = GroupId.From(GroupIdFactory.ForCommunity(message.Provider, message.PlatformId));

        var group = await db.Groups.FindAsync(groupId);
        if (group is not null)
        {
            group.Name = message.CommunityName;
        }
        else
        {
            group = new GroupEntity
            {
                Id = groupId,
                Name = message.CommunityName,
                AllowSharing = false,
            };
            db.Groups.Add(group);
        }

        return new CommunityRegistered(group.Id.Value, message.Provider, message.PlatformId);
    }
}
