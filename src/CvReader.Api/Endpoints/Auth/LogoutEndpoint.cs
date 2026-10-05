using CvReader.Api.Auth;
using CvReader.Application.Auth;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Auth;

public class LogoutEndpoint : EndpointWithoutRequest
{
    private readonly AuthService _authService;

    public LogoutEndpoint(AuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("/api/auth/logout");
        Summary(s => s.Summary = "Sign out: invalidates the session token and clears the cookie");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await _authService.LogoutAsync(User.GetUserId(), ct);
        AuthCookie.Delete(HttpContext.Response);
        await Send.NoContentAsync(ct);
    }
}
