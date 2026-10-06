using CvReader.Api.Auth;
using CvReader.Application.Abstractions;
using CvReader.Application.Cv;
using FastEndpoints;
using FluentValidation;

namespace CvReader.Api.Endpoints.Cv;

public class ListCvsRequest
{
    public Guid? FolderId { get; set; }
    public bool Unfiled { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ListCvsValidator : Validator<ListCvsRequest>
{
    public ListCvsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).InclusiveBetween(1, 10_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class ListCvsEndpoint : Endpoint<ListCvsRequest, CvPageDto>
{
    private readonly CvLibraryService _libraryService;

    public ListCvsEndpoint(CvLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    public override void Configure()
    {
        Get("/api/cv");
        Summary(s =>
        {
            s.Summary = "List the signed-in user's CVs, newest first";
            s.Description = "Filter by folder (folderId), by unfiled CVs (unfiled=true) or by file name, candidate name or email (search). Paged.";
        });
    }

    public override async Task HandleAsync(ListCvsRequest req, CancellationToken ct)
    {
        var filter = new ProfileFilter(req.FolderId, req.Unfiled, req.Search);
        await Send.OkAsync(await _libraryService.GetPageAsync(User.GetUserId(), filter, req.Page, req.PageSize, ct), ct);
    }
}
