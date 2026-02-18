namespace Hermod.Data.Entities;

public class UploadEntity
{
    public UploadId Id { get; set; }
    public UserId? UploadedById { get; set; }
    public required byte[] FileBytes { get; set; }
    public required string FileName { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity? UploadedBy { get; set; }
    public List<PlayEntity> Plays { get; set; } = [];
}
