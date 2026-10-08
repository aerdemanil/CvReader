using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Matching;

// FullName: CV'den çıkarılan aday adı; çıkarım bitene kadar ya da bulunamazsa null'dır.
public record MatchResultDto(Guid ProfileId, string FileName, string? FullName, double Score);

public record MatchPageDto(int Total, List<MatchResultDto> Items);

public record TermMatchDto(string Term, double Score);

public record KeywordMatchDto(string Keyword, bool Required, double Score, List<TermMatchDto> Terms);

public record MatchDetailDto(Guid ProfileId, string FileName, string? FullName, double Score, List<KeywordMatchDto> Keywords);

public class MatchingService
{
    private const int MaxTermsPerKeyword = 5;

    // Zorunlu anahtar kelime ortalamaya tercih edilenin iki katı ağırlıkla girer.
    private const double RequiredWeight = 2;

    // Zorunlu bir anahtar kelimesi hiç bulunmayan CV, diğerleri tam olsa da bu skoru geçemez.
    private const double MissingRequiredCap = 50;

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
            .GroupBy(s => (s.ProfileId, s.FileName, s.FullName))
            .Select(g => new MatchResultDto(
                g.Key.ProfileId,
                g.Key.FileName,
                g.Key.FullName,
                TotalScore(job, KeywordScores(job, g.ToDictionary(s => s.JobTerm, s => s.Similarity)))))
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

                return new KeywordMatchDto(keyword, job.RequiredKeywords.Contains(keyword), Math.Round(scores[index], 1), terms);
            })
            .ToList();

        return new MatchDetailDto(profile.Id, profile.FileName, profile.FullName, TotalScore(job, scores), keywords);
    }

    // Anahtar kelimenin skoru, terimlerinin skorlarının ortalamasıdır: "sql server" CV'de bütün olarak aranır,
    // "ci/cd" için hem "ci" hem "cd" aranır. CV'de yakını bulunmayan terim sözlükte yoktur ve 0 sayılır.
    private static List<double> KeywordScores(JobPosting job, Dictionary<string, double> bestSimilarities) =>
        job.Keywords
            .Select(keyword => TermExtractor.ExtractFromKeyword(keyword)
                .Average(term => SimilarityScorer.ToScore(bestSimilarities.GetValueOrDefault(term))))
            .ToList();

    // CV'nin skoru, anahtar kelime skorlarının ağırlıklı ortalamasıdır.
    private static double TotalScore(JobPosting job, List<double> keywordScores)
    {
        var required = job.Keywords.Select(job.RequiredKeywords.Contains).ToList();
        var weights = required.Select(r => r ? RequiredWeight : 1).ToList();

        var average = keywordScores.Zip(weights, (score, weight) => score * weight).Sum() / weights.Sum();
        var missingRequired = keywordScores.Zip(required, (score, isRequired) => isRequired && score == 0).Any(missing => missing);

        return Math.Round(missingRequired ? Math.Min(average, MissingRequiredCap) : average, 1);
    }
}
