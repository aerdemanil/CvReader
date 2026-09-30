using CvReader.Application.Matching;
using CvReader.Domain.Enums;

namespace CvReader.Tests;

public class KeywordMatcherTests
{
    [Fact]
    public void Java_does_not_match_JavaScript()
    {
        var result = KeywordMatcher.Match("5 years of JavaScript experience", ["Java"]);

        Assert.Empty(result.Matched);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Matching_ignores_case_and_turkish_i()
    {
        var result = KeywordMatcher.Match("İLERİ SEVİYE LINUX ve Docker bilgisi", ["ileri seviye", "Linux", "docker"]);

        Assert.Equal(3, result.Matched.Count);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Keywords_with_symbols_are_matched()
    {
        var result = KeywordMatcher.Match("Skills: C#, .NET, C++", ["C#", ".NET", "C++", "Go"]);

        Assert.Equal(["C#", ".NET", "C++"], result.Matched);
        Assert.Equal(["Go"], result.Missing);
        Assert.Equal(75, result.Score);
    }

    [Theory]
    [InlineData(100, MatchTier.K1)]
    [InlineData(80, MatchTier.K1)]
    [InlineData(79.9, MatchTier.K2)]
    [InlineData(50, MatchTier.K2)]
    [InlineData(49.9, MatchTier.K3)]
    [InlineData(0, MatchTier.K3)]
    public void Score_is_mapped_to_tier(double score, MatchTier expected)
    {
        Assert.Equal(expected, KeywordMatcher.ToTier(score));
    }
}
