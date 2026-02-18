using Hermod.Api.Handlers;
using Hermod.Api.Mappers;
using Hermod.Contracts.Plays;
using Hermod.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;

namespace Hermod.Api.Endpoints.Plays;

public static class UploadPlays
{
    [Authorize]
    [WolverinePost("/api/plays/upload")]
    public static async Task<PlayResponse[]> Post(
        IFormFile file,
        [FromQuery] Guid uploadedById,
        [FromQuery] Guid? groupId,
        [FromQuery] string? imageUrl,
        IMessageBus bus)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadPlaysCommand(
            stream,
            UserId.From(uploadedById),
            groupId.HasValue ? GroupId.From(groupId.Value) : null,
            imageUrl);

        var entities = await bus.InvokeAsync<List<Data.Entities.PlayEntity>>(command);
        return ResponseMapper.ToResponseArray(entities);
    }
}
