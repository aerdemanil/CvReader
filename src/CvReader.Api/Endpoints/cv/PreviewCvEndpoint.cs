using CvReader.Api.Services;
using FastEndpoints;

namespace CvReader.Api.Endpoints.cv;

public class PreviewCvEndpoint : EndpointWithoutRequest<ParsedCv>

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

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (Files.Count == 0)
        {
            AddError("No file provided.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var file = Files[0];

        if (file.ContentType != "application/pdf")
        {
            AddError("Invalid file format. Only PDF files are allowed.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        using var stream = file.OpenReadStream();
        var parsedCv = _cvParserService.Parse(stream);

        await Send.OkAsync(parsedCv, ct);
    }
}
