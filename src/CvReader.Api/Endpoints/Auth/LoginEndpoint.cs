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
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginEndpoint : Endpoint<LoginRequest, AuthResult>
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
        Summary(s => s.Summary = "Log in with email and password and get a JWT");
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(req.Email, req.Password, ct);

        if (result is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
