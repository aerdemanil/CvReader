using CvReader.Api.Auth;
using CvReader.Application.Folders;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Folders;

public class DeleteFolderRequest
{
    public Guid Id { get; set; }
}

public class DeleteFolderEndpoint : Endpoint<DeleteFolderRequest>
{
    private readonly FolderService _folderService;

    public DeleteFolderEndpoint(FolderService folderService)
    {
        _folderService = folderService;
    }

    public override void Configure()
    {
        Delete("/api/folders/{id}");
        Summary(s =>
        {
            s.Summary = "Delete a folder";
            s.Description = "The CVs inside are kept and become unfiled.";
        });
    }

    public override async Task HandleAsync(DeleteFolderRequest req, CancellationToken ct)
    {
        if (!await _folderService.DeleteAsync(User.GetUserId(), req.Id, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
