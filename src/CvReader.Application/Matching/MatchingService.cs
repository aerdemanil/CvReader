using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Matching;

public record MatchResultDto(
    Guid ProfileId,
    string FileName,
    double Score,
    string Tier,
    List<string> MatchedKeywords,
    List<string> MissingKeywords);

public class MatchingService
{
    private readonly IJobPostingRepository _jobs;
    private readonly IProfileRepository _profiles;
    private readonly IMatchResultRepository _results;

    public MatchingService(IJobPostingRepository jobs, IProfileRepository profiles, IMatchResultRepository results)
    {
        _jobs = jobs;
        _profiles = profiles;
        _results = results;
    }

    // İlan bulunamazsa null döner.
    public async Task<List<MatchResultDto>?> RunAsync(Guid jobPostingId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobPostingId, ct);
        if (job is null) return null;

        var profiles = await _profiles.GetAllAsync(ct);

        var results = profiles.Select(profile =>
        {
            var match = KeywordMatcher.Match(profile.RawText, job.Keywords);
            return new MatchResult
            {
                Id = Guid.NewGuid(),
                JobPostingId = job.Id,
                ProfileId = profile.Id,
                Score = match.Score,
                Tier = match.Tier,
                MatchedKeywords = match.Matched,
                MissingKeywords = match.Missing
            };
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
        return results.Select(r => new MatchResultDto(
            r.ProfileId,
            r.Profile.FileName,
            r.Score,
            r.Tier.ToString(),
            r.MatchedKeywords,
            r.MissingKeywords)).ToList();
    }
}
