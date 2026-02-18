using Hermod.Api.Mappers;
using Hermod.Contracts.Users;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Users;

public static class GetUser
{
    public static async Task<UserEntity?> LoadAsync(Guid id, HermodContext db)
        => await db.Users.FirstOrDefaultAsync(u => u.Id == UserId.From(id));

    [Authorize]
    [WolverineGet("/api/users/{id}")]
    public static UserResponse Get(UserEntity user)
        => ResponseMapper.ToResponse(user);
}
