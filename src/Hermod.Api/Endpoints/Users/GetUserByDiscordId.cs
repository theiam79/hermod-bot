using Hermod.Api.Mappers;
using Hermod.Contracts.Users;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Users;

public static class GetUserByDiscordId
{
    public static async Task<UserEntity?> LoadAsync(ulong discordId, HermodContext db)
        => await db.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);

    [Authorize]
    [WolverineGet("/api/users/by-discord/{discordId}")]
    public static UserResponse Get(UserEntity user)
        => ResponseMapper.ToResponse(user);
}
