using System.Text.RegularExpressions;
using CvReader.Application.Abstractions;
using CvReader.Application.Matching;
using CvReader.Domain.Entities;

namespace CvReader.Application.Jobs;

public record JobPostingDto(Guid Id, string Title, List<string> Keywords, DateTime CreatedAt);

public class JobPostingService
{
    private readonly IJobPostingRepository _jobs;
    private readonly IEmbeddingService _embeddings;

    public JobPostingService(IJobPostingRepository jobs, IEmbeddingService embeddings)
    {
        _jobs = jobs;
        _embeddings = embeddings;
    }

    public async Task<JobPostingDto> CreateAsync(Guid ownerId, string title, IEnumerable<string> keywords, CancellationToken ct)
    {
        var job = new JobPosting
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = title.Trim(),
            Keywords = keywords
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .DistinctBy(Normalize)
                .ToList()
        };

        // Anahtar kelimeler değişmediği için terim vektörleri bir kez alınır ve ilanla birlikte saklanır.
        var terms = job.Keywords.SelectMany(TermExtractor.ExtractFromKeyword).Distinct().ToList();
        var embeddings = await _embeddings.EmbedAsync(terms, ct);

        await _jobs.AddAsync(job, embeddings, ct);
        return ToDto(job);
    }

    public async Task<JobPostingDto?> GetAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(ownerId, id, ct);
        return job is null ? null : ToDto(job);
    }

    public async Task<List<JobPostingDto>> GetAllAsync(Guid ownerId, CancellationToken ct)
    {
        var jobs = await _jobs.GetAllAsync(ownerId, ct);
        return jobs.Select(ToDto).ToList();
    }

    // İlan bulunamazsa false döner.
    public Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        _jobs.DeleteAsync(ownerId, id, ct);

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
