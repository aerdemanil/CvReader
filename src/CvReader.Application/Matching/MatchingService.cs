using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Matching;

public record MatchResultDto(Guid ProfileId, string FileName, double Score);

public class MatchingService
{
    private readonly IJobPostingRepository _jobs;
    private readonly IProfileRepository _profiles;
    private readonly IMatchResultRepository _results;
    private readonly IEmbeddingService _embeddings;

    public MatchingService(
        IJobPostingRepository jobs,
        IProfileRepository profiles,
        IMatchResultRepository results,
        IEmbeddingService embeddings)
    {
        _jobs = jobs;
        _profiles = profiles;
        _results = results;
        _embeddings = embeddings;
    }

    // İlan bulunamazsa null döner.
    public async Task<List<MatchResultDto>?> RunAsync(Guid jobPostingId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobPostingId, ct);
        if (job is null) return null;

        var jobEmbedding = await _embeddings.EmbedAsync(string.Join(", ", job.Keywords), ct);
        var similarities = await _profiles.GetSimilaritiesAsync(jobEmbedding, ct);

        var results = similarities.Select(s => new MatchResult
        {
            Id = Guid.NewGuid(),
            JobPostingId = job.Id,
            ProfileId = s.Key,
            Score = SimilarityScorer.ToScore(s.Value)
        }).ToList();

        await _results.ReplaceForJobAsync(job.Id, results, ct);
        return await GetResultsAsync(job.Id, ct);
    }

    // İlan bulunamazsa null döner.
    public async Task<List<MatchResultDto>?> GetResultsAsync(Guid jobPostingId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobPostingId, ct);
        if (job is null) return null;

        var results = await _results.GetByJobAsync(jobPostingId, ct);
        return results
            .Select(r => new MatchResultDto(r.ProfileId, r.Profile.FileName, r.Score))
            .ToList();
    }
}
