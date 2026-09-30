namespace CvReader.Api.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
