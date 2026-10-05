using CvReader.Api.Auth;
using CvReader.Application.Folders;
using FastEndpoints;
using FluentValidation;

namespace CvReader.Api.Endpoints.Folders;

public class CreateFolderRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateFolderValidator : Validator<CreateFolderRequest>
{
    public CreateFolderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateFolderEndpoint : Endpoint<CreateFolderRequest, FolderDto>
{
    private readonly FolderService _folderService;

    public CreateFolderEndpoint(FolderService folderService)
    {
        _folderService = folderService;
    }

    public override void Configure()
    {
        Post("/api/folders");
        Options(x => x.RequireRateLimiting(RateLimits.Write));
        Summary(s => s.Summary = "Create a folder for organizing CVs");
    }

    public override async Task HandleAsync(CreateFolderRequest req, CancellationToken ct)
    {
        try
        {
            await Send.OkAsync(await _folderService.CreateAsync(User.GetUserId(), req.Name, ct), ct);
        }
        catch (FolderNameExistsException ex)
        {
            AddError(r => r.Name, ex.Message);
            await Send.ErrorsAsync(409, ct);
        }
    }
}
