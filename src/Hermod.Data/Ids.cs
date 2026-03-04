using Vogen;

namespace Hermod.Data;

[ValueObject<Guid>]
public readonly partial struct UserId;

[ValueObject<Guid>]
public readonly partial struct GroupId;

[ValueObject<Guid>]
public readonly partial struct PlayId;

[ValueObject<Guid>]
public readonly partial struct PlayPlayerId;

[ValueObject<Guid>]
public readonly partial struct PlayerMappingId;

[ValueObject<Guid>]
public readonly partial struct UploadId;
