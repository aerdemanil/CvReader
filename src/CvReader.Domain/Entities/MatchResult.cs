using CvReader.Domain.Enums;

namespace CvReader.Domain.Entities;

public class MatchResult
{
    public Guid Id { get; set; }

    public Guid JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;

    public double Score { get; set; }
    public MatchTier Tier { get; set; }
    public List<string> MatchedKeywords { get; set; } = [];
    public List<string> MissingKeywords { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
