namespace Hermod.Messages;

public record UnclaimPlayer(Guid UserId, string BgStatsPlayerUuid);

public record UnclaimPlayerResult(UnclaimStatus Status);

public enum UnclaimStatus { Removed, NotFound, Forbidden }
