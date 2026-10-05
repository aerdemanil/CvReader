using CvReader.Api.Auth;
using CvReader.Application.Jobs;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Jobs;

public class DeleteJobEndpoint : Endpoint<JobIdRequest>
{
    private readonly JobPostingService _jobService;

    public DeleteJobEndpoint(JobPostingService jobService)
    {
        _jobService = jobService;
    }

    public override void Configure()
    {
        Delete("/api/jobs/{id}");
        Summary(s => s.Summary = "Delete a job posting");
    }

    public override async Task HandleAsync(JobIdRequest req, CancellationToken ct)
    {
        if (!await _jobService.DeleteAsync(User.GetUserId(), req.Id, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
