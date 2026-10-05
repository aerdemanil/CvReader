using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Matching;

public record MatchResultDto(Guid ProfileId, string FileName, double Score);

public record MatchPageDto(int Total, List<MatchResultDto> Items);

public record TermMatchDto(string Term, double Score);

public record KeywordMatchDto(string Keyword, double Score, List<TermMatchDto> Terms);

public record MatchDetailDto(Guid ProfileId, string FileName, double Score, List<KeywordMatchDto> Keywords);

public class MatchingService
{
    private const int MaxTermsPerKeyword = 5;

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
        var job = await _jobs.GetByIdAsync(ownerId, jobPostingId, ct);
        if (job is null) return null;

        var similarities = await _profiles.GetBestSimilaritiesAsync(ownerId, jobPostingId, ct);

        var ranked = similarities
            .GroupBy(s => (s.ProfileId, s.FileName))
            .Select(g => new MatchResultDto(
                g.Key.ProfileId,
                g.Key.FileName,
                TotalScore(KeywordScores(job, g.ToDictionary(s => s.JobTerm, s => s.Similarity)))))
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.ProfileId) // eşit skorlarda sayfalar arası sıra sabit kalsın
            .ToList();

        var items = ranked.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new MatchPageDto(ranked.Count, items);
    }

    // Skorun nereden geldiğini gösterir: her anahtar kelime için CV'de ona en yakın bulunan terimler.
    // İlan ya da CV bulunamazsa null döner.
    public async Task<MatchDetailDto?> GetDetailAsync(Guid ownerId, Guid jobPostingId, Guid profileId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(ownerId, jobPostingId, ct);
        if (job is null) return null;

        var profile = await _profiles.GetByIdAsync(ownerId, profileId, ct);
        if (profile is null) return null;

        var matches = await _profiles.GetCloseTermsAsync(ownerId, jobPostingId, profileId, SimilarityScorer.Floor, ct);

        var best = matches
            .GroupBy(m => m.JobTerm)
            .ToDictionary(g => g.Key, g => g.Max(m => m.Similarity));
        var scores = KeywordScores(job, best);

        var keywords = job.Keywords
            .Select((keyword, index) =>
            {
                var jobTerms = TermExtractor.ExtractFromKeyword(keyword);

                var terms = matches
                    .Where(m => jobTerms.Contains(m.JobTerm))
                    .GroupBy(m => m.CvTerm)
                    .Select(g => new TermMatchDto(g.Key, SimilarityScorer.ToScore(g.Max(m => m.Similarity))))
                    .Where(t => t.Score > 0)
                    .OrderByDescending(t => t.Score)
                    .ThenBy(t => t.Term)
                    .Take(MaxTermsPerKeyword)
                    .ToList();

                return new KeywordMatchDto(keyword, Math.Round(scores[index], 1), terms);
            })
            .ToList();

        return new MatchDetailDto(profile.Id, profile.FileName, TotalScore(scores), keywords);
    }

    // Anahtar kelimenin skoru, terimlerinin skorlarının ortalamasıdır: "sql server" için CV'de hem "sql" hem "server" aranır.
    // CV'de yakını bulunmayan terim sözlükte yoktur ve 0 sayılır.
    private static List<double> KeywordScores(JobPosting job, Dictionary<string, double> bestSimilarities) =>
        job.Keywords
            .Select(keyword => TermExtractor.ExtractFromKeyword(keyword)
                .Average(term => SimilarityScorer.ToScore(bestSimilarities.GetValueOrDefault(term))))
            .ToList();

    // CV'nin skoru, anahtar kelime skorlarının ortalamasıdır.
    private static double TotalScore(List<double> keywordScores) =>
        Math.Round(keywordScores.Average(), 1);
}
