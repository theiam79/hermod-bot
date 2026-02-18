using Hermod.Contracts.Groups;
using Hermod.Contracts.Plays;
using Hermod.Contracts.Users;
using Hermod.Data;
using Hermod.Data.Entities;
using Riok.Mapperly.Abstractions;

namespace Hermod.Api.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ResponseMapper
{
    public static partial PlayResponse ToResponse(PlayEntity entity);
    public static partial PlayResponse[] ToResponseArray(List<PlayEntity> entities);
    public static partial UserResponse ToResponse(UserEntity entity);
    public static partial GroupResponse ToResponse(GroupEntity entity);

    private static partial PlayPlayerResponse ToResponse(PlayPlayerEntity entity);

    private static Guid ToGuid(PlayId id) => id.Value;
    private static Guid ToGuid(UserId id) => id.Value;
    private static Guid ToGuid(GroupId id) => id.Value;
    private static Guid? ToGuid(UserId? id) => id?.Value;
    private static Guid? ToGuid(PlayPlayerId id) => id.Value;
}
