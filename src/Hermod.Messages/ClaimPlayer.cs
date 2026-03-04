namespace Hermod.Messages;

public record ClaimPlayer(string Provider, string ProviderKey, string BgStatsPlayerUuid, Guid PlayId);

public record ClaimPlayerResult(ClaimPlayerStatus Status, string? PlayerName, Guid? UserId = null, string? ClaimantDisplayName = null);

public enum ClaimPlayerStatus { Claimed, AlreadyClaimed, IsUploader, NotRegistered, PlayerNotFound }
