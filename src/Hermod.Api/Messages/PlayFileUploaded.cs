namespace Hermod.Api.Messages;

public record PlayFileUploaded(Guid UploadId, Guid? GroupId, Guid? MePlayerUuid);
