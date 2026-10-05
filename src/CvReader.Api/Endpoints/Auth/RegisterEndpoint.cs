using System.Text;
using CvReader.Api.Auth;
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
        // bcrypt şifrenin yalnızca ilk 72 baytını kullanır; Türkçe karakterler UTF-8'de 2 bayt tutar.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Must(p => Encoding.UTF8.GetByteCount(p) <= 72)
            .WithMessage("Password must be at most 72 bytes long.");
    }
}

public class RegisterEndpoint : Endpoint<RegisterRequest, SessionResponse>
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
        Options(x => x.RequireRateLimiting(RateLimits.Auth));
        Summary(s => s.Summary = "Create an account; the session is set as an HttpOnly cookie");
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(req.Email, req.Password, ct);
            AuthCookie.Append(HttpContext.Response, result.Token, result.ExpiresAt);
            await Send.OkAsync(new SessionResponse(result.Email), ct);
        }
        catch (EmailAlreadyExistsException ex)
        {
            AddError(r => r.Email, ex.Message);
            await Send.ErrorsAsync(409, ct);
        }
    }
}
