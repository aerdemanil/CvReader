using CvReader.Application.Cv;
using CvReader.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace CvReader.Tests;

public class CvFieldExtractionServiceTests
{
    private readonly FakeProfileRepository _profiles = new(new FakeJobPostingRepository());
    private readonly FakeCvNameExtractor _names = new();
    private readonly CvFieldExtractionService _service;

    public CvFieldExtractionServiceTests()
    {
        _service = new CvFieldExtractionService(_profiles, _names, NullLogger<CvFieldExtractionService>.Instance);
    }

    private Profile AddProfile(string rawText, int minutesAgo = 0)
    {
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            RawText = rawText,
            CreatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo)
        };
        _profiles.Profiles.Add((profile, []));
        return profile;
    }

    [Fact]
    public async Task Name_email_and_phone_are_saved_and_the_cv_leaves_the_queue()
    {
        var profile = AddProfile("Ada Lovelace\nada@example.com\n0532 111 22 33\nBackend developer");
        _names.Result = "Ada Lovelace";

        var processed = await _service.ProcessPendingAsync(5, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal("Ada Lovelace", profile.FullName);
        Assert.Equal("ada@example.com", profile.Email);
        Assert.Equal("05321112233", profile.Phone);
        Assert.NotNull(profile.FieldsExtractedAt);
        Assert.Equal(0, await _service.ProcessPendingAsync(5, CancellationToken.None));
    }

    [Fact]
    public async Task Name_that_is_not_written_in_the_cv_is_rejected()
    {
        var profile = AddProfile("Ada Lovelace\nBackend developer");
        _names.Result = "Grace Hopper";

        await _service.ProcessPendingAsync(5, CancellationToken.None);

        Assert.Null(profile.FullName);
        Assert.NotNull(profile.FieldsExtractedAt);
    }

    [Theory]
    [InlineData("AYŞE YILMAZ\nMühendis", "Ayşe Yılmaz")]
    [InlineData("Ayşe\nYılmaz Mühendis", "Ayşe Yılmaz")]
    public async Task Name_matches_despite_letter_case_and_line_breaks(string rawText, string modelAnswer)
    {
        var profile = AddProfile(rawText);
        _names.Result = modelAnswer;

        await _service.ProcessPendingAsync(5, CancellationToken.None);

        Assert.Equal(modelAnswer, profile.FullName);
    }

    [Theory]
    [InlineData("ada@example.com")]
    [InlineData("Ada 2024")]
    [InlineData("A")]
    public async Task Answer_that_does_not_look_like_a_name_is_rejected(string modelAnswer)
    {
        var profile = AddProfile($"{modelAnswer} Backend developer");
        _names.Result = modelAnswer;

        await _service.ProcessPendingAsync(5, CancellationToken.None);

        Assert.Null(profile.FullName);
    }

    [Fact]
    public async Task Only_the_beginning_of_the_cv_is_sent_to_the_name_extractor()
    {
        AddProfile(new string('x', 5000));

        await _service.ProcessPendingAsync(5, CancellationToken.None);

        Assert.Equal(1500, Assert.Single(_names.Inputs).Length);
    }

    [Fact]
    public async Task Cv_stays_pending_while_the_name_service_is_unreachable()
    {
        var profile = AddProfile("Ada Lovelace\nada@example.com");
        _names.Fail = true;

        var processed = await _service.ProcessPendingAsync(5, CancellationToken.None);

        Assert.Equal(0, processed);
        Assert.Null(profile.Email);
        Assert.Null(profile.FieldsExtractedAt);
    }

    [Fact]
    public async Task Oldest_cvs_are_processed_first_up_to_the_batch_size()
    {
        var newest = AddProfile("newest", minutesAgo: 1);
        var oldest = AddProfile("oldest", minutesAgo: 3);
        var middle = AddProfile("middle", minutesAgo: 2);

        var processed = await _service.ProcessPendingAsync(2, CancellationToken.None);

        Assert.Equal(2, processed);
        Assert.NotNull(oldest.FieldsExtractedAt);
        Assert.NotNull(middle.FieldsExtractedAt);
        Assert.Null(newest.FieldsExtractedAt);
    }
}
