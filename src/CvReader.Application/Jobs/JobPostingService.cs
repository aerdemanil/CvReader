using System.Text.RegularExpressions;
using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Jobs;

public record JobPostingDto(Guid Id, string Title, List<string> Keywords, DateTime CreatedAt);

public class JobPostingService
{
    private readonly IJobPostingRepository _jobs;

    public JobPostingService(IJobPostingRepository jobs)
    {
        _jobs = jobs;
    }

    public async Task<JobPostingDto> CreateAsync(string title, IEnumerable<string> keywords, CancellationToken ct)
    {
        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Keywords = keywords
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .DistinctBy(Normalize)
                .ToList()
        };

        await _jobs.AddAsync(job, ct);
        return ToDto(job);
    }

    public async Task<JobPostingDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(id, ct);
        return job is null ? null : ToDto(job);
    }

    public async Task<List<JobPostingDto>> GetAllAsync(CancellationToken ct)
    {
        var jobs = await _jobs.GetAllAsync(ct);
        return jobs.Select(ToDto).ToList();
    }

    private static JobPostingDto ToDto(JobPosting job) =>
        new(job.Id, job.Title, job.Keywords, job.CreatedAt);

    // "Java", "java " ve "JAVA" aynı anahtar kelime sayılsın; Türkçe İ/ı da katlanır.
    private static string Normalize(string value)
    {
        var folded = value
            .Replace('İ', 'i')
            .Replace('I', 'i')
            .Replace('ı', 'i')
            .ToLowerInvariant();

        return Regex.Replace(folded, @"\s+", " ").Trim();
    }
}
