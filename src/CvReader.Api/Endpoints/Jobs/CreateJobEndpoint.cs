using CvReader.Application.Jobs;
using FastEndpoints;
using FluentValidation;

namespace CvReader.Api.Endpoints.Jobs;

public class CreateJobRequest
{
    public string Title { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
}

public class CreateJobValidator : Validator<CreateJobRequest>
{
    public CreateJobValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Keywords)
            .Must(k => k.Any(w => !string.IsNullOrWhiteSpace(w)))
            .WithMessage("At least one keyword is required.");
    }
}

public class CreateJobEndpoint : Endpoint<CreateJobRequest, JobPostingDto>
{
    private readonly JobPostingService _jobService;

    public CreateJobEndpoint(JobPostingService jobService)
    {
        _jobService = jobService;
    }

    public override void Configure()
    {
        Post("/api/jobs");
        Summary(s => s.Summary = "Create a job posting with keywords");
    }

    public override async Task HandleAsync(CreateJobRequest req, CancellationToken ct)
    {
        var job = await _jobService.CreateAsync(req.Title, req.Keywords, ct);
        await Send.OkAsync(job, ct);
    }
}
