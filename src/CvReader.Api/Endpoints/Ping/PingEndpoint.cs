using FastEndpoints;

namespace CvReader.Api.Endpoints.Ping;



public class PingEndpoint : EndpointWithoutRequest<string>

{

    public override void Configure()

    {

        Get("/api/ping");

        AllowAnonymous();


    }

    public override async Task HandleAsync(CancellationToken ct)

        => await Send.OkAsync("pong", ct);
}