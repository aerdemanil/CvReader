using CvReader.Application.Cv;

namespace CvReader.Tests;

public class ContactExtractorTests
{
    [Fact]
    public void First_email_is_returned_in_lower_case()
    {
        Assert.Equal("ada.lovelace@example.com", ContactExtractor.ExtractEmail("Ada Lovelace | Ada.Lovelace@Example.com | ref: boss@corp.com"));
    }

    [Fact]
    public void Punctuation_around_the_email_is_not_part_of_it()
    {
        Assert.Equal("ada@example.com", ContactExtractor.ExtractEmail("E-posta: <ada@example.com>."));
    }

    [Fact]
    public void Text_without_email_returns_null()
    {
        Assert.Null(ContactExtractor.ExtractEmail("Twitter: @ada, no address here"));
    }

    [Fact]
    public void Email_longer_than_the_column_is_not_truncated()
    {
        var email = $"{new string('a', ContactExtractor.MaxEmailLength)}@example.com";

        Assert.Null(ContactExtractor.ExtractEmail(email));
    }

    [Theory]
    [InlineData("Tel: 0532 111 22 33", "05321112233")]
    [InlineData("+90 (532) 111 22 33", "+905321112233")]
    [InlineData("Phone: +44 20 7946 0958", "+442079460958")]
    [InlineData("GSM 532-111-22-33", "5321112233")]
    public void Phone_is_normalized_to_digits_and_leading_plus(string text, string expected)
    {
        Assert.Equal(expected, ContactExtractor.ExtractPhone(text));
    }

    [Theory]
    [InlineData("Software Engineer 2019 - 2023")]
    [InlineData("2015-2019 2019-2023")]
    [InlineData("Born 01.01.1990, graduated 30.06.2012")]
    [InlineData("TC 12345678901")]
    public void Years_dates_and_identity_numbers_are_not_phones(string text)
    {
        Assert.Null(ContactExtractor.ExtractPhone(text));
    }

    [Fact]
    public void Phone_is_found_after_numbers_that_are_not_phones()
    {
        Assert.Equal("05321112233", ContactExtractor.ExtractPhone($"2019 - 2023 Acme{Environment.NewLine}0532 111 22 33"));
    }
}
