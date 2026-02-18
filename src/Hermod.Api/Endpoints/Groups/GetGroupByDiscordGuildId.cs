using Hermod.Api.Mappers;
using Hermod.Contracts.Groups;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Groups;

public static class GetGroupByDiscordGuildId
{
    public static async Task<GroupEntity?> LoadAsync(ulong guildId, HermodContext db)
        => await db.Groups.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId);

    [Authorize]
    [WolverineGet("/api/groups/by-discord/{guildId}")]
    public static GroupResponse Get(GroupEntity group)
        => ResponseMapper.ToResponse(group);
}
