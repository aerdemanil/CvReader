using CvReader.Application.Matching;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Jobs;

public class GetMatchResultsEndpoint : Endpoint<JobIdRequest, List<MatchResultDto>>
{
    private readonly MatchingService _matchingService;

    public GetMatchResultsEndpoint(MatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    public override void Configure()
    {
        Get("/api/jobs/{id}/results");
        AllowAnonymous();
        Summary(s => s.Summary = "Get saved match results for a job posting");
    }

    public override async Task HandleAsync(JobIdRequest req, CancellationToken ct)
    {
        var results = await _matchingService.GetResultsAsync(req.Id, ct);

        if (results is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(results, ct);
    }
}
