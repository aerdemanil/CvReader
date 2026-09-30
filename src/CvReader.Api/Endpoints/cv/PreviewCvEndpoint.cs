using CvReader.Api.Services;
using FastEndpoints;
using UglyToad.PdfPig.Core;

namespace CvReader.Api.Endpoints.cv;

public class PreviewCvRequest
{
    public IFormFile File { get; set; } = default!;
}

public class PreviewCvEndpoint : Endpoint<PreviewCvRequest, ParsedCv>
{
    private readonly CvParserService _cvParserService;

    public PreviewCvEndpoint(CvParserService cvParserService)
    {
        _cvParserService = cvParserService;
    }

    public override void Configure()
    {
        Post("/api/cv/preview");
        AllowAnonymous();
        AllowFileUploads();
        Summary(s =>
        {
            s.Summary = "Preview CV";
            s.Description = "Extracts text from a PDF CV and returns the text along with the number of pages.";
            s.Response<ParsedCv>(200, "Returns the extracted text and page count.");
            s.Response(400, "Invalid file format or no file provided.");
        });
    }

    public override async Task HandleAsync(PreviewCvRequest req, CancellationToken ct)
    {
        var file = req.File;

        if (file is null || file.Length == 0)
        {
            AddError("No file provided.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (file.ContentType != "application/pdf")
        {
            AddError("Invalid file format. Only PDF files are allowed.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        ParsedCv parsedCv;
        try
        {
            using var stream = file.OpenReadStream();
            parsedCv = _cvParserService.Parse(stream);
        }
        catch (PdfDocumentFormatException)
        {
            AddError("The file could not be read as a valid PDF.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(parsedCv, ct);
    }
}
