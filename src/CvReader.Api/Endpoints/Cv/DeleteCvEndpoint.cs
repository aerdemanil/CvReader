using CvReader.Api.Auth;
using CvReader.Application.Cv;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Cv;

public class CvIdRequest
{
    public Guid Id { get; set; }
}

public class DeleteCvEndpoint : Endpoint<CvIdRequest>
{
    private readonly CvLibraryService _libraryService;

    public DeleteCvEndpoint(CvLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    public override void Configure()
    {
        Delete("/api/cv/{id}");
        Summary(s => s.Summary = "Delete an uploaded CV together with its text and embedding");
    }

    public override async Task HandleAsync(CvIdRequest req, CancellationToken ct)
    {
        if (!await _libraryService.DeleteAsync(User.GetUserId(), req.Id, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
