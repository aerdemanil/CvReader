using CvReader.Application.Embeddings;
using CvReader.Application.Jobs;

namespace CvReader.Tests;

public class JobPostingServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private readonly FakeJobPostingRepository _jobs = new();
    private readonly FakeEmbeddingService _embeddings = new();
    private readonly JobPostingService _service;

    public JobPostingServiceTests()
    {
        _service = new JobPostingService(_jobs, _embeddings);
    }

    [Fact]
    public async Task Create_trims_and_removes_duplicate_keywords()
    {
        var job = await _service.CreateAsync(Owner, "  Backend  ", ["Java", " java ", "JAVA", "", "BİLGİ", "bilgi", "Kotlin"], [], CancellationToken.None);

        Assert.Equal("Backend", job.Title);
        Assert.Equal(["Java", "BİLGİ", "Kotlin"], job.Keywords);
    }

    [Fact]
    public async Task Create_keeps_only_required_keywords_that_are_among_the_keywords()
    {
        var job = await _service.CreateAsync(Owner, "Backend", ["Java", "Kotlin", "SQL"], [" java ", "SQL", "Go"], CancellationToken.None);

        Assert.Equal(["Java", "SQL"], job.RequiredKeywords);
        Assert.Equal(["Java", "SQL"], Assert.Single(_jobs.Jobs).Job.RequiredKeywords);
    }

    [Fact]
    public async Task Create_embeds_every_term_of_the_keywords_once_and_stores_the_vectors_with_the_job()
    {
        _embeddings.Result = [0.5f, 0.5f];

        await _service.CreateAsync(Owner, "Backend", ["Java", "SQL Server", "sql"], [], CancellationToken.None);

        Assert.Equal(["java", "sql server", "sql"], Assert.Single(_embeddings.Inputs));
        var stored = Assert.Single(_jobs.Jobs);
        Assert.Equal(Owner, stored.Job.OwnerId);
        Assert.Equal(["java", "sql server", "sql"], stored.Terms.Select(t => t.Term));
        Assert.All(stored.Terms, t => Assert.Equal([0.5f, 0.5f], t.Embedding));
    }

    [Fact]
    public async Task Create_saves_nothing_when_embedding_fails()
    {
        _embeddings.Fail = true;

        await Assert.ThrowsAsync<EmbeddingException>(() =>
            _service.CreateAsync(Owner, "Backend", ["Java"], [], CancellationToken.None));

        Assert.Empty(_jobs.Jobs);
    }

    [Fact]
    public async Task Jobs_of_another_user_are_not_visible()
    {
        var job = await _service.CreateAsync(Owner, "Backend", ["Java"], [], CancellationToken.None);

        Assert.Null(await _service.GetAsync(Stranger, job.Id, CancellationToken.None));
        Assert.Empty(await _service.GetAllAsync(Stranger, CancellationToken.None));
        Assert.Single(await _service.GetAllAsync(Owner, CancellationToken.None));
    }

    [Fact]
    public async Task Only_the_owner_can_delete_a_job()
    {
        var job = await _service.CreateAsync(Owner, "Backend", ["Java"], [], CancellationToken.None);

        Assert.False(await _service.DeleteAsync(Stranger, job.Id, CancellationToken.None));
        Assert.Single(_jobs.Jobs);

        Assert.True(await _service.DeleteAsync(Owner, job.Id, CancellationToken.None));
        Assert.Empty(_jobs.Jobs);
    }
}
