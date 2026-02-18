namespace Hermod.Api.Messages;

public record PlayFileUploaded(Guid UploadId, Guid? GroupId, string? SenderDiscordId, Guid? MePlayerUuid);
