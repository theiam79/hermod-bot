namespace Hermod.Data.Entities;

public class UploadEntity
{
    public UploadId Id { get; set; }
    public UserId UploadedById { get; set; }
    public required string FileContent { get; set; }
    public required string FileName { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<PlayEntity> Plays { get; set; } = [];
}
