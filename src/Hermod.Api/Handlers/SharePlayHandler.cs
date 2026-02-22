using Hermod.Api.Messages;
using Hermod.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hermod.Api.Handlers;

public static class SharePlayHandler
{
    public static async Task<OutgoingMessages> Handle(PlayPersisted message, HermodContext db)
    {
        var uploadedById = UserId.From(message.UploadedById);

        var groupIds = await db.UserGroups
            .Where(ug => ug.UserId == uploadedById && ug.Group.AllowSharing)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        var messages = new OutgoingMessages();
        foreach (var groupId in groupIds)
        {
            messages.Add(new SharePlayToGroup(message.PlayId, groupId.Value, message.ChangeType));
        }

        return messages;
    }
}
