namespace CvReader.Domain.Entities;

public class MatchResult
{
    public Guid Id { get; set; }

    public Guid JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;

    public double Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
