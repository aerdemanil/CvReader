using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public interface IMatchResultRepository
{
    Task ReplaceForJobAsync(Guid jobPostingId, List<MatchResult> results, CancellationToken ct);
    Task<List<MatchResult>> GetByJobAsync(Guid jobPostingId, CancellationToken ct);
}