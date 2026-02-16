using Hermod.Core.Models;
using Hermod.Data.Entities;

namespace Hermod.Core.Mappers;

public static class UserMapper
{
    public static UserProfile ToProfile(UserEntity entity)
    {
        return new UserProfile
        {
            Id = entity.Id,
            DisplayName = entity.DisplayName,
            DiscordId = entity.DiscordId,
            BggId = entity.BggId,
            BggUsername = entity.BggUsername,
            SubscribeToPlays = entity.SubscribeToPlays,
        };
    }
}
