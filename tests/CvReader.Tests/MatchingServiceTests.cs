using CvReader.Application.Abstractions;
using CvReader.Application.Matching;
using CvReader.Domain.Entities;

namespace CvReader.Tests;

public class MatchingServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    // Birbirine dik vektörler: aynı terim 1, farklı terimler 0 benzerlik verir.
    private static readonly float[] Java = [1, 0, 0, 0];
    private static readonly float[] SqlServer = [0, 1, 0, 0];
    private static readonly float[] Docker = [0, 0, 1, 0];
    private static readonly float[] Linux = [0, 0, 0, 1];

    // "sql server" ile benzerliği Floor ve Ceiling'in tam ortasındadır.
    private static readonly float[] Sql = [0, 1, 0.659f, 0];

    private readonly FakeJobPostingRepository _jobs = new();
    private readonly FakeProfileRepository _profiles;
    private readonly MatchingService _service;
    private readonly Guid _jobId;

    public MatchingServiceTests()
    {
        _profiles = new FakeProfileRepository(_jobs);
        _service = new MatchingService(_jobs, _profiles);
        _jobId = AddJob(["Java", "SQL Server"], [], new("java", Java), new("sql server", SqlServer));
    }

    private Guid AddJob(List<string> keywords, List<string> requiredKeywords, params TermEmbedding[] terms)
    {
        var job = new JobPosting { Id = Guid.NewGuid(), OwnerId = Owner, Title = "Backend", Keywords = keywords, RequiredKeywords = requiredKeywords };
        _jobs.Jobs.Add((job, terms));
        return job.Id;
    }

    private Guid AddProfile(Guid ownerId, string fileName, params TermEmbedding[] terms)
    {
        var profile = new Profile { Id = Guid.NewGuid(), OwnerId = ownerId, FileName = fileName };
        _profiles.Profiles.Add((profile, terms));
        return profile.Id;
    }

    private async Task<double> ScoreAsync(Guid jobId)
    {
        var page = await _service.GetResultsAsync(Owner, jobId, 1, 50, CancellationToken.None);
        return Assert.Single(page!.Items).Score;
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
        AddProfile(Owner, "none.pdf", new TermEmbedding("excel", [1, 1, 1, 1]));
        AddProfile(Owner, "all.pdf", new("java", Java), new("sql server", SqlServer));
        AddProfile(Owner, "half.pdf", new TermEmbedding("java", Java));
        AddProfile(Stranger, "not-mine.pdf", new TermEmbedding("java", Java));

        var page = await _service.GetResultsAsync(Owner, _jobId, 1, 50, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(3, page.Total);
        Assert.Equal(["all.pdf", "half.pdf", "none.pdf"], page.Items.Select(i => i.FileName));
        Assert.Equal([100, 50, 0], page.Items.Select(i => i.Score));
    }

    [Fact]
    public async Task Close_term_scores_less_than_the_keyword_itself()
    {
        AddProfile(Owner, "sql.pdf", new("java", Java), new("sql", Sql));

        // "Java" 100, "SQL Server" yakın terimle 40; ortalama 70.
        Assert.Equal(70, await ScoreAsync(_jobId));
    }

    [Fact]
    public async Task Keyword_of_several_terms_scores_the_average_of_its_terms()
    {
        var jobId = AddJob(["CI/CD"], [], new("ci", Java), new("cd", Docker));
        AddProfile(Owner, "ci-only.pdf", new TermEmbedding("ci", Java));

        Assert.Equal(50, await ScoreAsync(jobId));
    }

    [Fact]
    public async Task Required_keyword_weighs_twice_as_much()
    {
        var jobId = AddJob(["Java", "SQL Server"], ["Java"], new("java", Java), new("sql server", SqlServer));
        AddProfile(Owner, "java-only.pdf", new TermEmbedding("java", Java));

        // (100 * 2 + 0) / 3
        Assert.Equal(66.7, await ScoreAsync(jobId));
    }

    [Fact]
    public async Task Cv_without_a_required_keyword_cannot_score_above_the_cap()
    {
        var jobId = AddJob(
            ["Java", "SQL Server", "Docker", "Linux"], ["Java"],
            new("java", Java), new("sql server", SqlServer), new("docker", Docker), new("linux", Linux));
        AddProfile(Owner, "no-java.pdf", new("sql server", SqlServer), new("docker", Docker), new("linux", Linux));

        // Ağırlıklı ortalama (0 * 2 + 300) / 5 = 60 olurdu.
        Assert.Equal(50, await ScoreAsync(jobId));
    }

    [Fact]
    public async Task Results_are_paged_and_total_counts_every_cv()
    {
        AddProfile(Owner, "first.pdf", new("java", Java), new("sql server", SqlServer));
        AddProfile(Owner, "second.pdf", new TermEmbedding("java", Java));
        AddProfile(Owner, "third.pdf", new TermEmbedding("excel", [1, 1, 1, 1]));

        var page = await _service.GetResultsAsync(Owner, _jobId, 2, 2, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(3, page.Total);
        Assert.Equal("third.pdf", Assert.Single(page.Items).FileName);
    }

    [Fact]
    public async Task Detail_lists_the_closest_cv_terms_of_every_keyword()
    {
        var jobId = AddJob(["Java", "SQL Server"], ["SQL Server"], new("java", Java), new("sql server", SqlServer));
        var profileId = AddProfile(Owner, "ada.pdf", new("java", Java), new("sql", Sql), new("excel", [1, 1, 1, 1]));

        var detail = await _service.GetDetailAsync(Owner, jobId, profileId, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("ada.pdf", detail.FileName);
        Assert.Equal(60, detail.Score);

        Assert.Equal(["Java", "SQL Server"], detail.Keywords.Select(k => k.Keyword));
        Assert.Equal([false, true], detail.Keywords.Select(k => k.Required));
        Assert.Equal([100, 40], detail.Keywords.Select(k => k.Score));
        Assert.Equal([new TermMatchDto("java", 100)], detail.Keywords[0].Terms);
        Assert.Equal([new TermMatchDto("sql", 40)], detail.Keywords[1].Terms);
    }

    [Fact]
    public async Task Detail_score_equals_the_score_in_the_ranked_list()
    {
        var profileId = AddProfile(Owner, "ada.pdf", new("java", [1, 0.2f, 0, 0]), new("sql", Sql));

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
