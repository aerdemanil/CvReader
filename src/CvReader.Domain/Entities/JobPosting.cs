namespace CvReader.Domain.Entities;

public class JobPosting
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}