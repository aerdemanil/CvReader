using CvReader.Application.Jobs;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Jobs;

public class ListJobsEndpoint : EndpointWithoutRequest<List<JobPostingDto>>
{
    private readonly JobPostingService _jobService;

    public ListJobsEndpoint(JobPostingService jobService)
    {
        _jobService = jobService;
    }

    public override void Configure()
    {
        Get("/api/jobs");
        Summary(s => s.Summary = "List job postings, newest first");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(await _jobService.GetAllAsync(ct), ct);
    }
}
