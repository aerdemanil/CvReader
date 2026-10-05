using CvReader.Application.Abstractions;
using CvReader.Application.Matching;
using CvReader.Domain.Entities;

namespace CvReader.Tests;

public class MatchingServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    // Birbirine dik vektörler: aynı terim 1, farklı terimler 0 benzerlik verir.
    private static readonly float[] Java = [1, 0, 0];
    private static readonly float[] Sql = [0, 1, 0];
    private static readonly float[] Server = [0, 0, 1];

    private readonly FakeJobPostingRepository _jobs = new();
    private readonly FakeProfileRepository _profiles;
    private readonly MatchingService _service;
    private readonly Guid _jobId = Guid.NewGuid();

    public MatchingServiceTests()
    {
        _profiles = new FakeProfileRepository(_jobs);
        _service = new MatchingService(_jobs, _profiles);
        _jobs.Jobs.Add((
            new JobPosting { Id = _jobId, OwnerId = Owner, Title = "Backend", Keywords = ["Java", "SQL Server"] },
            [new("java", Java), new("sql", Sql), new("server", Server)]));
    }

    private Guid AddProfile(Guid ownerId, string fileName, params TermEmbedding[] terms)
    {
        var profile = new Profile { Id = Guid.NewGuid(), OwnerId = ownerId, FileName = fileName };
        _profiles.Profiles.Add((profile, terms));
        return profile.Id;
    }

    [Fact]
    public async Task Unknown_job_returns_null()
    {
        Assert.Null(await _service.GetResultsAsync(Owner, Guid.NewGuid(), 1, 50, CancellationToken.None));
    }

    [Fact]
    public async Task Job_of_another_user_returns_null()
    {
        Assert.Null(await _service.GetResultsAsync(Stranger, _jobId, 1, 50, CancellationToken.None));
    }

    [Fact]
    public async Task Only_the_owners_cvs_are_ranked_by_how_many_keywords_they_cover()
    {
        AddProfile(Owner, "none.pdf", new TermEmbedding("excel", [1, 1, 1]));
        AddProfile(Owner, "all.pdf", new("java", Java), new("sql", Sql), new("server", Server));
        AddProfile(Owner, "half.pdf", new TermEmbedding("java", Java));
        AddProfile(Stranger, "not-mine.pdf", new TermEmbedding("java", Java));

        var page = await _service.GetResultsAsync(Owner, _jobId, 1, 50, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(3, page.Total);
        Assert.Equal(["all.pdf", "half.pdf", "none.pdf"], page.Items.Select(i => i.FileName));
        Assert.Equal([100, 50, 0], page.Items.Select(i => i.Score));
    }

    [Fact]
    public async Task Keyword_of_several_terms_scores_the_average_of_its_terms()
    {
        AddProfile(Owner, "sql-only.pdf", new TermEmbedding("sql", Sql));

        var page = await _service.GetResultsAsync(Owner, _jobId, 1, 50, CancellationToken.None);

        // "Java" 0, "SQL Server" (100 + 0) / 2 = 50; ortalama 25.
        Assert.Equal(25, Assert.Single(page!.Items).Score);
    }

    [Fact]
    public async Task Results_are_paged_and_total_counts_every_cv()
    {
        AddProfile(Owner, "first.pdf", new("java", Java), new("sql", Sql), new("server", Server));
        AddProfile(Owner, "second.pdf", new TermEmbedding("java", Java));
        AddProfile(Owner, "third.pdf", new TermEmbedding("excel", [1, 1, 1]));

        var page = await _service.GetResultsAsync(Owner, _jobId, 2, 2, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(3, page.Total);
        Assert.Equal("third.pdf", Assert.Single(page.Items).FileName);
    }

    [Fact]
    public async Task Detail_lists_the_closest_cv_terms_of_every_keyword()
    {
        var profileId = AddProfile(Owner, "ada.pdf", new("java", Java), new("server", Server), new("excel", [1, 1, 1]));

        var detail = await _service.GetDetailAsync(Owner, _jobId, profileId, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("ada.pdf", detail.FileName);
        Assert.Equal(75, detail.Score);

        Assert.Equal(["Java", "SQL Server"], detail.Keywords.Select(k => k.Keyword));
        Assert.Equal([100, 50], detail.Keywords.Select(k => k.Score));
        Assert.Equal([new TermMatchDto("java", 100)], detail.Keywords[0].Terms);
        Assert.Equal([new TermMatchDto("server", 100)], detail.Keywords[1].Terms);
    }

    [Fact]
    public async Task Detail_score_equals_the_score_in_the_ranked_list()
    {
        var profileId = AddProfile(Owner, "ada.pdf", new("java", [1, 0.2f, 0]), new("sql", [0, 1, 0.3f]));

        var page = await _service.GetResultsAsync(Owner, _jobId, 1, 50, CancellationToken.None);
        var detail = await _service.GetDetailAsync(Owner, _jobId, profileId, CancellationToken.None);

        Assert.Equal(Assert.Single(page!.Items).Score, detail!.Score);
    }

    [Fact]
    public async Task Detail_of_another_users_cv_or_job_returns_null()
    {
        var mine = AddProfile(Owner, "mine.pdf", new TermEmbedding("java", Java));
        var theirs = AddProfile(Stranger, "theirs.pdf", new TermEmbedding("java", Java));

        Assert.Null(await _service.GetDetailAsync(Owner, _jobId, theirs, CancellationToken.None));
        Assert.Null(await _service.GetDetailAsync(Stranger, _jobId, mine, CancellationToken.None));
        Assert.Null(await _service.GetDetailAsync(Owner, _jobId, Guid.NewGuid(), CancellationToken.None));
    }
}
