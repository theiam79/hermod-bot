namespace Hermod.Messages;

public record GetUserClaims(Guid UserId);

public record GetUserClaimsResult(List<UserClaimInfo> Claims);

public record UserClaimInfo(string BgStatsPlayerUuid, string PlayerName, int PlayCount);
