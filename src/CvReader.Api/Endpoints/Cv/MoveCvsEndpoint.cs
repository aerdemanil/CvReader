using CvReader.Api.Auth;
using CvReader.Application.Cv;
using FastEndpoints;
using FluentValidation;

namespace CvReader.Api.Endpoints.Cv;

public class MoveCvsRequest
{
    public List<Guid> ProfileIds { get; set; } = [];

    // null: CV'ler klasörden çıkarılır.
    public Guid? FolderId { get; set; }
}

public class MoveCvsValidator : Validator<MoveCvsRequest>
{
    public MoveCvsValidator()
    {
        RuleFor(x => x.ProfileIds)
            .Must(ids => ids.Count is > 0 and <= 100)
            .WithMessage("Between 1 and 100 CVs can be moved at once.");
    }
}

public class MoveCvsEndpoint : Endpoint<MoveCvsRequest>
{
    private readonly CvLibraryService _libraryService;

    public MoveCvsEndpoint(CvLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    public override void Configure()
    {
        Put("/api/cv/folder");
        Summary(s => s.Summary = "Move CVs into a folder, or out of any folder when folderId is null");
    }

    public override async Task HandleAsync(MoveCvsRequest req, CancellationToken ct)
    {
        if (!await _libraryService.MoveAsync(User.GetUserId(), req.ProfileIds, req.FolderId, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
