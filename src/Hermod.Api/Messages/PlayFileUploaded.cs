namespace Hermod.Api.Messages;

public record PlayFileUploaded(Guid UploadId, Guid? MePlayerUuid, Guid UploadedById);
