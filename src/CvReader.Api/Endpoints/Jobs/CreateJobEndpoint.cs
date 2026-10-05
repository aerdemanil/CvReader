using CvReader.Api.Auth;
using CvReader.Application.Embeddings;
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
            .WithMessage("At least one keyword is required.")
            .Must(k => k.Count <= 30)
            .WithMessage("At most 30 keywords are allowed.");
        RuleForEach(x => x.Keywords).MaximumLength(100); // kolon varchar(100)
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
        Options(x => x.RequireRateLimiting(RateLimits.Write));
        Summary(s => s.Summary = "Create a job posting with keywords");
    }

    public override async Task HandleAsync(CreateJobRequest req, CancellationToken ct)
    {
        try
        {
            var job = await _jobService.CreateAsync(User.GetUserId(), req.Title, req.Keywords, ct);
            await Send.OkAsync(job, ct);
        }
        catch (EmbeddingException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(503, ct);
        }
    }
}
