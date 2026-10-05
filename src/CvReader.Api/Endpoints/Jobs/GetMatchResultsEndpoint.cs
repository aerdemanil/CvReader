using CvReader.Api.Auth;
using CvReader.Application.Matching;
using FastEndpoints;
using FluentValidation;

namespace CvReader.Api.Endpoints.Jobs;

public class GetMatchResultsRequest
{
    public Guid Id { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetMatchResultsValidator : Validator<GetMatchResultsRequest>
{
    public GetMatchResultsValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 10_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetMatchResultsEndpoint : Endpoint<GetMatchResultsRequest, MatchPageDto>
{
    private readonly MatchingService _matchingService;

    public GetMatchResultsEndpoint(MatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    public override void Configure()
    {
        Get("/api/jobs/{id}/results");
        Summary(s =>
        {
            s.Summary = "Rank the signed-in user's CVs against a job posting";
            s.Description = "Scores are calculated on every read, so newly uploaded CVs are included automatically. Sorted by score, paged.";
        });
    }

    public override async Task HandleAsync(GetMatchResultsRequest req, CancellationToken ct)
    {
        var results = await _matchingService.GetResultsAsync(User.GetUserId(), req.Id, req.Page, req.PageSize, ct);

        if (results is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(results, ct);
    }
}
