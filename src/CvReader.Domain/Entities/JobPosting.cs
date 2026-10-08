namespace CvReader.Domain.Entities;

public class JobPosting
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];

    // Keywords içinden zorunlu sayılanlar; geri kalanı tercih sebebidir.
    public List<string> RequiredKeywords { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
