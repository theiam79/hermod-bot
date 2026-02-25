namespace Hermod.Messages;

public record ClaimPlayer(string DiscordId, string BgStatsPlayerUuid, Guid PlayId);

public record ClaimPlayerResult(ClaimPlayerStatus Status, string? PlayerName);

public enum ClaimPlayerStatus { Claimed, AlreadyClaimed, IsUploader, NotRegistered, PlayerNotFound }
