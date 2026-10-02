using System.Text.RegularExpressions;
using CvReader.Domain.Enums;

namespace CvReader.Application.Matching;

public record KeywordMatch(double Score, MatchTier Tier, List<string> Matched, List<string> Missing);

public static class KeywordMatcher
{
    public const double K1Threshold = 80;
    public const double K2Threshold = 50;

    public static KeywordMatch Match(string cvText, IReadOnlyList<string> keywords)
    {
        var text = Normalize(cvText);
        var matched = new List<string>();
        var missing = new List<string>();

        foreach (var keyword in keywords)
        {
            if (ContainsWord(text, Normalize(keyword)))
                matched.Add(keyword);
            else
                missing.Add(keyword);
        }

        var score = keywords.Count == 0
            ? 0
            : Math.Round(100.0 * matched.Count / keywords.Count, 1);

        return new KeywordMatch(score, ToTier(score), matched, missing);
    }

    public static MatchTier ToTier(double score)
    {
        if (score >= K1Threshold) return MatchTier.K1;
        if (score >= K2Threshold) return MatchTier.K2;
        return MatchTier.K3;
    }

    public static string Normalize(string value)
    {
        var folded = value
            .Replace('İ', 'i')
            .Replace('I', 'i')
            .Replace('ı', 'i')
            .ToLowerInvariant();

        return Regex.Replace(folded, @"\s+", " ").Trim();
    }

    private static bool ContainsWord(string text, string word)
    {
        var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(word)}(?![\p{{L}}\p{{N}}])";
        return Regex.IsMatch(text, pattern);
    }
}
