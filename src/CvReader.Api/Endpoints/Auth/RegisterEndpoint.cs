using CvReader.Application.Auth;
using FastEndpoints;
using FluentValidation;

namespace CvReader.Api.Endpoints.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterValidator : Validator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(72);
    }
}

public class RegisterEndpoint : Endpoint<RegisterRequest, AuthResult>
{
    private readonly AuthService _authService;

    public RegisterEndpoint(AuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
        Summary(s => s.Summary = "Create an account and return a JWT");
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(req.Email, req.Password, ct);
            await Send.OkAsync(result, ct);
        }
        catch (EmailAlreadyExistsException ex)
        {
            AddError(r => r.Email, ex.Message);
            await Send.ErrorsAsync(409, ct);
        }
    }
}
