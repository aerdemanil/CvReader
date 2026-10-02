using CvReader.Application.Cv;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Cv;

public class BulkUploadCvRequest
{
    public List<IFormFile> Files { get; set; } = [];
}

public class BulkUploadCvEndpoint : Endpoint<BulkUploadCvRequest, List<CvUploadResult>>
{
    private readonly CvUploadService _uploadService;

    public BulkUploadCvEndpoint(CvUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    public override void Configure()
    {
        Post("/api/cv/bulk");
        AllowFileUploads();
        Summary(s =>
        {
            s.Summary = "Bulk upload CVs";
            s.Description = "Uploads several PDF CVs at once and saves each one as a profile. Returns a result per file.";
        });
    }

    public override async Task HandleAsync(BulkUploadCvRequest req, CancellationToken ct)
    {
        if (req.Files.Count == 0)
        {
            AddError("No files provided.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var results = new List<CvUploadResult>();

        foreach (var file in req.Files)
        {
            if (file.ContentType != "application/pdf")
            {
                results.Add(new CvUploadResult(file.FileName, null, "Only PDF files are allowed."));
                continue;
            }

            await using var stream = file.OpenReadStream();
            results.Add(await _uploadService.UploadAsync(file.FileName, stream, ct));
        }

        await Send.OkAsync(results, ct);
    }
}
