using CvReader.Application.Matching;

namespace CvReader.Tests;

public class TermExtractorTests
{
    [Fact]
    public void Terms_are_lowercased_and_distinct_in_first_seen_order()
    {
        Assert.Equal(["java", "developer"], TermExtractor.Extract("Java developer, JAVA Developer."));
    }

    [Fact]
    public void Technical_names_stay_in_one_piece()
    {
        Assert.Equal(["c#", "c++", ".net", "node.js", "back-end"], TermExtractor.Extract("C#, C++, .NET, Node.js; Back-End"));
    }

    [Fact]
    public void Numbers_single_letters_and_punctuation_are_not_terms()
    {
        Assert.Equal(["tel", "since"], TermExtractor.Extract("Tel: 0532 111 22 33 - a • since 2019"));
    }

    [Fact]
    public void Slashes_and_commas_separate_terms()
    {
        Assert.Equal(["ci", "cd", "ios"], TermExtractor.Extract("CI/CD,iOS"));
    }

    [Fact]
    public void Term_count_is_limited()
    {
        var text = string.Join(' ', Enumerable.Range(0, TermExtractor.MaxTerms + 50).Select(i => $"word{i}"));

        Assert.Equal(TermExtractor.MaxTerms, TermExtractor.Extract(text).Count);
    }

    [Fact]
    public void Keyword_is_split_like_cv_text()
    {
        Assert.Equal(["sql", "server"], TermExtractor.ExtractFromKeyword("SQL Server"));
    }

    [Fact]
    public void Keyword_that_cannot_be_split_is_used_as_it_is()
    {
        Assert.Equal(["c"], TermExtractor.ExtractFromKeyword("C"));
    }
}
