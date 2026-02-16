using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Core.Services;

public class PlayerMappingService(IDbContextFactory<HermodContext> contextFactory)
{
    public async Task CreateMappingAsync(
        UserId ownerId,
        string bgStatsPlayerUuid,
        UserId mappedUserId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var existing = await context.PlayerMappings.FirstOrDefaultAsync(
            pm => pm.OwnerUserId == ownerId && pm.BgStatsPlayerUuid == bgStatsPlayerUuid, ct);

        if (existing is not null)
        {
            existing.MappedUserId = mappedUserId;
        }
        else
        {
            context.PlayerMappings.Add(new PlayerMappingEntity
            {
                Id = PlayerMappingId.From(Guid.NewGuid()),
                OwnerUserId = ownerId,
                BgStatsPlayerUuid = bgStatsPlayerUuid,
                MappedUserId = mappedUserId,
            });
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<string, UserId>> GetMappingsForOwnerAsync(
        UserId ownerId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.PlayerMappings
            .Where(pm => pm.OwnerUserId == ownerId)
            .ToDictionaryAsync(pm => pm.BgStatsPlayerUuid, pm => pm.MappedUserId, ct);
    }

    public async Task DeleteMappingAsync(UserId ownerId, string bgStatsPlayerUuid, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var mapping = await context.PlayerMappings.FirstOrDefaultAsync(
            pm => pm.OwnerUserId == ownerId && pm.BgStatsPlayerUuid == bgStatsPlayerUuid, ct);

        if (mapping is not null)
        {
            context.PlayerMappings.Remove(mapping);
            await context.SaveChangesAsync(ct);
        }
    }
}
