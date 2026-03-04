namespace Hermod.Messages;

public record SharePlayToGroup(Guid PlayId, Guid GroupId, PlayChangeType ChangeType, PlaySnapshot Snapshot);
