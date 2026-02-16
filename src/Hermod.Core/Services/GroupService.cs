using Hermod.Core.Mappers;
using Hermod.Core.Models;
using Hermod.Data;
using Hermod.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hermod.Core.Services;

public class GroupService(IDbContextFactory<HermodContext> contextFactory)
{
    public async Task<GroupSummary> GetOrCreateByDiscordGuildAsync(
        ulong discordGuildId,
        string name,
        ulong? defaultChannelId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var group = await context.Groups.FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId, ct);
        if (group is not null)
        {
            return GroupMapper.ToSummary(group);
        }

        group = new GroupEntity
        {
            Id = GroupId.From(Guid.NewGuid()),
            Name = name,
            DiscordGuildId = discordGuildId,
            DiscordPostChannelId = defaultChannelId,
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync(ct);
        return GroupMapper.ToSummary(group);
    }

    public async Task<GroupSummary?> GetByIdAsync(GroupId id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id, ct);
        return group is null ? null : GroupMapper.ToSummary(group);
    }

    public async Task<List<GroupSummary>> GetGroupsForUserAsync(UserId userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var groups = await context.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.Group)
            .ToListAsync(ct);

        return groups.Select(GroupMapper.ToSummary).ToList();
    }

    public async Task<GroupSummary?> UpdateSettingsAsync(
        GroupId id,
        bool allowSharing,
        ulong? postChannelId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group is null) return null;

        group.AllowSharing = allowSharing;
        group.DiscordPostChannelId = postChannelId;

        await context.SaveChangesAsync(ct);
        return GroupMapper.ToSummary(group);
    }

    public async Task AddUserToGroupAsync(UserId userId, GroupId groupId, GroupRole role = GroupRole.Member, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var exists = await context.UserGroups.AnyAsync(
            ug => ug.UserId == userId && ug.GroupId == groupId, ct);

        if (exists) return;

        context.UserGroups.Add(new UserGroupEntity
        {
            UserId = userId,
            GroupId = groupId,
            Role = role,
        });

        await context.SaveChangesAsync(ct);
    }
}
