using System.Security.Claims;
using CvReader.Api.Auth;
using FastEndpoints;

namespace CvReader.Api.Endpoints.Auth;

public class MeEndpoint : EndpointWithoutRequest<SessionResponse>
{
    public override void Configure()
    {
        Get("/api/auth/me");
        Summary(s => s.Summary = "Get the signed-in user");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new SessionResponse(User.FindFirstValue(ClaimNames.Email)!), ct);
    }
}
