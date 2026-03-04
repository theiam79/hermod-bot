namespace Hermod.Messages;

public record UnclaimPlayer(Guid UserId, string BgStatsPlayerUuid);

public record UnclaimPlayerResult(UnclaimStatus Status, ClaimRemoved? Event = null);

public enum UnclaimStatus { Removed, NotFound, Forbidden }

public record ClaimRemoved(string BgStatsPlayerUuid, List<Guid> AffectedPlayIds);
