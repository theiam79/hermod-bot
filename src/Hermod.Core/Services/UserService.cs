using Hermod.Core.Mappers;
using Hermod.Core.Models;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Core.Services;

public class UserService(IDbContextFactory<HermodContext> contextFactory)
{
    public async Task<UserProfile> GetOrCreateByDiscordIdAsync(ulong discordId, string displayName, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var user = await context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId, ct);
        if (user is not null)
        {
            return UserMapper.ToProfile(user);
        }

        user = new UserEntity
        {
            Id = UserId.From(Guid.NewGuid()),
            DisplayName = displayName,
            DiscordId = discordId,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
        return UserMapper.ToProfile(user);
    }

    public async Task<UserProfile?> GetByIdAsync(UserId id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? null : UserMapper.ToProfile(user);
    }

    public async Task<UserProfile?> UpdatePreferencesAsync(
        UserId id,
        bool subscribeToPlays,
        int? bggId,
        string? bggUsername,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;

        user.SubscribeToPlays = subscribeToPlays;
        user.BggId = bggId;
        user.BggUsername = bggUsername;

        await context.SaveChangesAsync(ct);
        return UserMapper.ToProfile(user);
    }
}
