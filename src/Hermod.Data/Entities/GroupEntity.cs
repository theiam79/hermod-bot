namespace Hermod.Data.Entities;

public class GroupEntity
{
    public GroupId Id { get; set; }
    public required string Name { get; set; }
    public ulong? DiscordGuildId { get; set; }
    public ulong? DiscordPostChannelId { get; set; }
    public bool AllowSharing { get; set; } = false;

    /// <summary>
    /// Number of plays in a single upload at or above which a summary embed is posted
    /// instead of individual embeds. Defaults to 3.
    /// </summary>
    public int SpamThreshold { get; set; } = 3;

    public List<UserGroupEntity> UserGroups { get; set; } = [];
}
