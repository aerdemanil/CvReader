using CvReader.Application.Matching;
using CvReader.Domain.Entities;

namespace CvReader.Tests;

public class MatchingServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private readonly FakeJobPostingRepository _jobs = new();
    private readonly FakeProfileRepository _profiles = new();
    private readonly MatchingService _service;
    private readonly Guid _jobId = Guid.NewGuid();

    public MatchingServiceTests()
    {
        _service = new MatchingService(_jobs, _profiles);
        _jobs.Jobs.Add((new JobPosting { Id = _jobId, OwnerId = Owner, Title = "Backend" }, [1, 0]));
    }

    private void AddProfile(Guid ownerId, string fileName, float[] embedding) =>
        _profiles.Profiles.Add((new Profile { Id = Guid.NewGuid(), OwnerId = ownerId, FileName = fileName }, embedding));

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
    public async Task Only_the_owners_cvs_are_ranked_closest_first()
    {
        AddProfile(Owner, "far.pdf", [0, 1]);
        AddProfile(Owner, "close.pdf", [1, 0]);
        AddProfile(Stranger, "not-mine.pdf", [1, 0]);

        var page = await _service.GetResultsAsync(Owner, _jobId, 1, 50, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(2, page.Total);
        Assert.Equal(["close.pdf", "far.pdf"], page.Items.Select(i => i.FileName));
        Assert.Equal(100, page.Items[0].Score);
        Assert.Equal(0, page.Items[1].Score);
    }

    [Fact]
    public async Task Results_are_paged_and_total_counts_every_cv()
    {
        AddProfile(Owner, "first.pdf", [1, 0]);
        AddProfile(Owner, "second.pdf", [1, 0.5f]);
        AddProfile(Owner, "third.pdf", [0, 1]);

        var page = await _service.GetResultsAsync(Owner, _jobId, 2, 2, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(3, page.Total);
        Assert.Equal("third.pdf", Assert.Single(page.Items).FileName);
    }
}
