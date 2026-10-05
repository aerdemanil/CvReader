namespace CvReader.Domain.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }

    // null: CV hiçbir klasörde değil.
    public Guid? FolderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
