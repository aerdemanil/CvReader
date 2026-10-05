using CvReader.Api.Auth;
using CvReader.Application.Folders;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Folders;

public class ListFoldersEndpoint : EndpointWithoutRequest<FolderListDto>
{
    private readonly FolderService _folderService;

    public ListFoldersEndpoint(FolderService folderService)
    {
        _folderService = folderService;
    }

    public override void Configure()
    {
        Get("/api/folders");
        Summary(s => s.Summary = "List the signed-in user's folders with their CV counts");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(await _folderService.GetAllAsync(User.GetUserId(), ct), ct);
    }
}
