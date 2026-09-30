using CvReader.Domain.Enums;

namespace CvReader.Domain.Entities;

public class MatchResult
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public Guid JobPostingId { get; set; }
    public MatchTier Tier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}