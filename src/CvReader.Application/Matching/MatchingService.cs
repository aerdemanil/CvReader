using CvReader.Application.Abstractions;

namespace CvReader.Application.Matching;

public record MatchResultDto(Guid ProfileId, string FileName, double Score);

public record MatchPageDto(int Total, List<MatchResultDto> Items);

public class MatchingService
{
    private readonly IJobPostingRepository _jobs;
    private readonly IProfileRepository _profiles;

    public MatchingService(IJobPostingRepository jobs, IProfileRepository profiles)
    {
        _jobs = jobs;
        _profiles = profiles;
    }

    // Skorlar saklanmaz, her okumada hesaplanır; yeni yüklenen CV'ler kendiliğinden sonuca girer.
    // İlan bulunamazsa null döner.
    public async Task<MatchPageDto?> GetResultsAsync(Guid ownerId, Guid jobPostingId, int page, int pageSize, CancellationToken ct)
    {
        var jobEmbedding = await _jobs.GetEmbeddingAsync(ownerId, jobPostingId, ct);
        if (jobEmbedding is null) return null;

        var ranked = await _profiles.GetRankedAsync(ownerId, jobEmbedding, (page - 1) * pageSize, pageSize, ct);

        var items = ranked.Items
            .Select(p => new MatchResultDto(p.ProfileId, p.FileName, SimilarityScorer.ToScore(p.Similarity)))
            .ToList();

        return new MatchPageDto(ranked.Total, items);
    }
}
