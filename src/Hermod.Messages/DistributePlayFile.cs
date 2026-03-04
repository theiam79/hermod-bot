namespace Hermod.Messages;

public record DistributePlayFile(string FileContent, string FileName, Guid RecipientUserId);
