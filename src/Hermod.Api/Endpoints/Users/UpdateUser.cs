using Hermod.Api.Mappers;
using Hermod.Contracts.Users;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Users;

public static class UpdateUser
{
    public static async Task<UserEntity?> LoadAsync(Guid id, HermodContext db)
        => await db.Users.FirstOrDefaultAsync(u => u.Id == UserId.From(id));

    [Authorize]
    [WolverinePut("/api/users/{id}")]
    public static UserResponse Put(UpdateUserRequest request, UserEntity user)
    {
        user.SubscribeToPlays = request.SubscribeToPlays;
        user.BggId = request.BggId;
        user.BggUsername = request.BggUsername;
        return ResponseMapper.ToResponse(user);
    }
}
