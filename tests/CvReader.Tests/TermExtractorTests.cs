using CvReader.Application.Matching;

namespace CvReader.Tests;

public class TermExtractorTests
{
    [Fact]
    public void Terms_are_lowercased_and_distinct_in_first_seen_order()
    {
        Assert.Equal(["java", "developer", "java developer"], TermExtractor.Extract("Java developer, JAVA Developer."));
    }

    [Fact]
    public void Consecutive_words_also_form_phrases_of_up_to_three_words()
    {
        Assert.Equal(
            ["asp.net", "core", "asp.net core", "web", "core web", "asp.net core web", "api", "web api", "core web api"],
            TermExtractor.Extract("ASP.NET Core\nWeb API"));
    }

    [Fact]
    public void Punctuation_numbers_and_sentence_ends_break_phrases()
    {
        Assert.Equal(["java", "sql", "spring", "boot", "spring boot", "docker"], TermExtractor.Extract("Java, SQL. Spring Boot 3 Docker"));
    }

    [Fact]
    public void Alternative_spellings_become_one_term()
    {
        Assert.Equal(["c#", ".net", "node.js", "sql server"], TermExtractor.Extract("CSharp, dotnet, NodeJS, MSSQL"));
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
    public void Keyword_of_several_words_is_one_term()
    {
        Assert.Equal(["sql server"], TermExtractor.ExtractFromKeyword("SQL Server"));
    }

    [Fact]
    public void Keyword_is_split_where_cv_text_would_be()
    {
        Assert.Equal(["ci", "cd"], TermExtractor.ExtractFromKeyword("CI/CD"));
    }

    [Fact]
    public void Keyword_uses_the_same_spelling_as_cv_text()
    {
        Assert.Equal(["c#"], TermExtractor.ExtractFromKeyword("CSharp"));
    }

    [Fact]
    public void Keyword_that_cannot_be_split_is_used_as_it_is()
    {
        Assert.Equal(["c"], TermExtractor.ExtractFromKeyword("C"));
    }
}
