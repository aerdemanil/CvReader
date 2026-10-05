using CvReader.Api.Auth;
using CvReader.Application.Auth;
using FastEndpoints;
using FluentValidation;

namespace CvReader.Api.Endpoints.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(72);
    }
}

public class LoginEndpoint : Endpoint<LoginRequest, SessionResponse>
{
    private readonly AuthService _authService;

    public LoginEndpoint(AuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimits.Auth));
        Summary(s => s.Summary = "Log in with email and password; the session is set as an HttpOnly cookie");
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(req.Email, req.Password, ct);

        if (result is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        AuthCookie.Append(HttpContext.Response, result.Token, result.ExpiresAt);
        await Send.OkAsync(new SessionResponse(result.Email), ct);
    }
}
