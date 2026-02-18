using Hermod.Api.Mappers;
using Hermod.Contracts.Users;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Users;

public static class CreateUser
{
    [Authorize]
    [WolverinePost("/api/users")]
    public static async Task<IResult> Post(CreateUserRequest request, HermodContext db)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == request.DiscordId);
        if (existing is not null)
            return Results.Ok(ResponseMapper.ToResponse(existing));

        var user = new UserEntity
        {
            Id = UserId.From(Guid.NewGuid()),
            DisplayName = request.DisplayName,
            DiscordId = request.DiscordId,
        };

        db.Users.Add(user);
        var response = ResponseMapper.ToResponse(user);
        return Results.Created($"/api/users/{response.Id}", response);
    }
}
