using CvReader.Application.Cv;
using Microsoft.Extensions.Logging.Abstractions;

namespace CvReader.Tests;

public class CvUploadServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private readonly FakeCvParser _parser = new();
    private readonly FakeProfileRepository _profiles = new(new FakeJobPostingRepository());
    private readonly FakeEmbeddingService _embeddings = new();
    private readonly CvUploadService _service;

    public CvUploadServiceTests()
    {
        _service = new CvUploadService(_parser, _profiles, _embeddings, NullLogger<CvUploadService>.Instance);
    }

    private static MemoryStream File(string content) => new(System.Text.Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task Valid_cv_is_saved_for_its_owner()
    {
        var result = await _service.UploadAsync(Owner, null, "ada.pdf", File("cv-1"), CancellationToken.None);

        Assert.Null(result.Error);
        var saved = Assert.Single(_profiles.Profiles).Profile;
        Assert.Equal(result.ProfileId, saved.Id);
        Assert.Equal(Owner, saved.OwnerId);
        Assert.Equal("ada.pdf", saved.FileName);
        Assert.Equal(64, saved.ContentHash.Length);
    }

    [Fact]
    public async Task Every_distinct_term_of_the_cv_is_embedded_and_saved_with_it()
    {
        await _service.UploadAsync(Owner, null, "ada.pdf", File("cv-1"), CancellationToken.None);

        Assert.Equal(["parsed", "text", "parsed text"], Assert.Single(_embeddings.Inputs));
        Assert.Equal(["parsed", "text", "parsed text"], Assert.Single(_profiles.Profiles).Terms.Select(t => t.Term));
    }

    [Fact]
    public async Task Cv_without_words_returns_an_error_and_saves_nothing()
    {
        _parser.Text = "0532 111 22 33 —";

        var result = await _service.UploadAsync(Owner, null, "numbers.pdf", File("cv-1"), CancellationToken.None);

        Assert.NotNull(result.Error);
        Assert.Empty(_profiles.Profiles);
        Assert.Empty(_embeddings.Inputs);
    }

    [Fact]
    public async Task Invalid_pdf_returns_an_error_and_saves_nothing()
    {
        _parser.Fail = true;

        var result = await _service.UploadAsync(Owner, null, "broken.pdf", File("cv-1"), CancellationToken.None);

        Assert.NotNull(result.Error);
        Assert.Null(result.ProfileId);
        Assert.Empty(_profiles.Profiles);
        Assert.Empty(_embeddings.Inputs);
    }

    [Fact]
    public async Task Embedding_failure_returns_an_error_and_saves_nothing()
    {
        _embeddings.Fail = true;

        var result = await _service.UploadAsync(Owner, null, "ada.pdf", File("cv-1"), CancellationToken.None);

        Assert.NotNull(result.Error);
        Assert.Empty(_profiles.Profiles);
    }

    [Fact]
    public async Task Same_file_uploaded_twice_is_rejected_without_embedding_again()
    {
        await _service.UploadAsync(Owner, null, "ada.pdf", File("cv-1"), CancellationToken.None);

        var second = await _service.UploadAsync(Owner, null, "ada-copy.pdf", File("cv-1"), CancellationToken.None);

        Assert.NotNull(second.Error);
        Assert.Single(_profiles.Profiles);
        Assert.Single(_embeddings.Inputs);
    }

    [Fact]
    public async Task Same_file_can_be_uploaded_by_different_users()
    {
        await _service.UploadAsync(Owner, null, "ada.pdf", File("cv-1"), CancellationToken.None);

        var other = await _service.UploadAsync(Stranger, null, "ada.pdf", File("cv-1"), CancellationToken.None);

        Assert.Null(other.Error);
        Assert.Equal(2, _profiles.Profiles.Count);
    }
}
