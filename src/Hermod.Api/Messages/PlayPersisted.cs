namespace Hermod.Api.Messages;

public record PlayPersisted(Guid PlayId, Guid UploadedById, PlayChangeType ChangeType);
