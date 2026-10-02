using CvReader.Application.Jobs;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Jobs;

public class GetJobEndpoint : Endpoint<JobIdRequest, JobPostingDto>
{
    private readonly JobPostingService _jobService;

    public GetJobEndpoint(JobPostingService jobService)
    {
        _jobService = jobService;
    }

    public override void Configure()
    {
        Get("/api/jobs/{id}");
        Summary(s => s.Summary = "Get a job posting");
    }

    public override async Task HandleAsync(JobIdRequest req, CancellationToken ct)
    {
        var job = await _jobService.GetAsync(req.Id, ct);

        if (job is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(job, ct);
    }
}
