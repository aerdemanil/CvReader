using CvReader.Api.Auth;
using CvReader.Application.Cv;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Cv;

public class GetCvEndpoint : Endpoint<CvIdRequest, CvDetailDto>
{
    private readonly CvLibraryService _libraryService;

    public GetCvEndpoint(CvLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    public override void Configure()
    {
        Get("/api/cv/{id}");
        Summary(s => s.Summary = "Get a CV with its extracted text");
    }

    public override async Task HandleAsync(CvIdRequest req, CancellationToken ct)
    {
        var cv = await _libraryService.GetAsync(User.GetUserId(), req.Id, ct);

        if (cv is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(cv, ct);
    }
}
