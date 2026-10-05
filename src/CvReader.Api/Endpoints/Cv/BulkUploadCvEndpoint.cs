using CvReader.Api.Auth;
using CvReader.Application.Cv;
using CvReader.Application.Folders;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Cv;

public class BulkUploadCvRequest
{
    public List<IFormFile> Files { get; set; } = [];

    // Verilirse yüklenen tüm CV'ler bu klasöre konur.
    public Guid? FolderId { get; set; }
}

public class BulkUploadCvEndpoint : Endpoint<BulkUploadCvRequest, List<CvUploadResult>>
{
    private const int MaxFiles = 50;
    private const long MaxFileBytes = 10 * 1024 * 1024;
    private const int MaxFileNameLength = 260; // kolon varchar(260)

    private readonly CvUploadService _uploadService;
    private readonly FolderService _folderService;

    public BulkUploadCvEndpoint(CvUploadService uploadService, FolderService folderService)
    {
        _uploadService = uploadService;
        _folderService = folderService;
    }

    public override void Configure()
    {
        Post("/api/cv/bulk");
        AllowFileUploads();
        Options(x => x.RequireRateLimiting(RateLimits.Upload));
        Summary(s =>
        {
            s.Summary = "Bulk upload CVs";
            s.Description = "Uploads several PDF CVs at once and saves each one as a profile. Returns a result per file.";
        });
    }

    public override async Task HandleAsync(BulkUploadCvRequest req, CancellationToken ct)
    {
        if (req.Files.Count is 0 or > MaxFiles)
        {
            AddError($"Between 1 and {MaxFiles} files must be provided.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var ownerId = User.GetUserId();

        if (req.FolderId is not null && !await _folderService.ExistsAsync(ownerId, req.FolderId.Value, ct))
        {
            AddError(r => r.FolderId, "The folder does not exist.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var results = new List<CvUploadResult>();

        // Bir dosyadaki sorun diğerlerini etkilemez; her dosya kendi sonuç satırını alır.
        // İçerik türü istemcinin beyanı olduğu için ona güvenilmez; PDF olup olmadığına ayrıştırıcı imzadan karar verir.
        foreach (var file in req.Files)
        {
            var fileName = Path.GetFileName(file.FileName);

            if (fileName.Length is 0 or > MaxFileNameLength)
            {
                results.Add(new CvUploadResult(fileName, null, $"The file name must be between 1 and {MaxFileNameLength} characters."));
                continue;
            }

            if (file.Length is 0 or > MaxFileBytes)
            {
                results.Add(new CvUploadResult(fileName, null, "The file is empty or larger than 10 MB."));
                continue;
            }

            await using var stream = file.OpenReadStream();
            results.Add(await _uploadService.UploadAsync(ownerId, req.FolderId, fileName, stream, ct));
        }

        await Send.OkAsync(results, ct);
    }
}
