using Hermod.Api.Mappers;
using Hermod.Contracts.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Groups;

public static class UpdateGroupSettings
{
    public static async Task<GroupEntity?> LoadAsync(Guid id, HermodContext db)
        => await db.Groups.FirstOrDefaultAsync(g => g.Id == GroupId.From(id));

    [Authorize]
    [WolverinePut("/api/groups/{id}/settings")]
    public static GroupResponse Put(UpdateGroupSettingsRequest request, GroupEntity group)
    {
        group.AllowSharing = request.AllowSharing;
        group.DiscordPostChannelId = request.PostChannelId;
        return ResponseMapper.ToResponse(group);
    }
}
