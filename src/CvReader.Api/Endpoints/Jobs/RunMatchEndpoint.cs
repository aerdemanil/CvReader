using CvReader.Application.Embeddings;
using CvReader.Application.Matching;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Jobs;

public class RunMatchEndpoint : Endpoint<JobIdRequest, List<MatchResultDto>>
{
    private readonly MatchingService _matchingService;

    public RunMatchEndpoint(MatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    public override void Configure()
    {
        Post("/api/jobs/{id}/match");
        Summary(s =>
        {
            s.Summary = "Score all profiles against a job posting";
            s.Description = "Recalculates and saves the score of every profile, then returns them sorted by score.";
        });
    }

    public override async Task HandleAsync(JobIdRequest req, CancellationToken ct)
    {
        List<MatchResultDto>? results;
        try
        {
            results = await _matchingService.RunAsync(req.Id, ct);
        }
        catch (EmbeddingException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(503, ct);
            return;
        }

        if (results is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(results, ct);
    }
}
