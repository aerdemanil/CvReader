using CvReader.Api.Auth;
using CvReader.Application.Matching;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Jobs;

public class GetMatchDetailRequest
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
}

public class GetMatchDetailEndpoint : Endpoint<GetMatchDetailRequest, MatchDetailDto>
{
    private readonly MatchingService _matchingService;

    public GetMatchDetailEndpoint(MatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    public override void Configure()
    {
        Get("/api/jobs/{id}/results/{profileId}");
        Summary(s =>
        {
            s.Summary = "Explain a CV's score for a job posting";
            s.Description = "For every keyword of the job: its score and the terms of the CV that were found closest to it.";
        });
    }

    public override async Task HandleAsync(GetMatchDetailRequest req, CancellationToken ct)
    {
        var detail = await _matchingService.GetDetailAsync(User.GetUserId(), req.Id, req.ProfileId, ct);

        if (detail is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(detail, ct);
    }
}
