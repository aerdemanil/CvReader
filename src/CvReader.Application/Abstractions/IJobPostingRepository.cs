using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public interface IJobPostingRepository
{
    Task AddAsync(JobPosting job, CancellationToken ct);
    Task<JobPosting?> GetByIdAsync(Guid id, CancellationToken ct);
}