using Hermod.Data;
using Hermod.Messages;

namespace Hermod.Api.Handlers;

public static class UpdateGroupSharingHandler
{
    public static async Task Handle(UpdateGroupSharing message, HermodContext db)
    {
        var group = await db.Groups.FindAsync(GroupId.From(message.GroupId));
        if (group is null)
            return;

        group.AllowSharing = message.AllowSharing;
    }
}
