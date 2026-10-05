using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;
using FastEndpoints.Security;
using Microsoft.Extensions.Options;

namespace CvReader.Api.Auth;

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken CreateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = _options.SigningKey;
            o.Issuer = _options.Issuer;
            o.Audience = _options.Audience;
            o.ExpireAt = expiresAt;
            o.User.Claims.Add((ClaimNames.UserId, user.Id.ToString()));
            o.User.Claims.Add((ClaimNames.Email, user.Email));
            o.User.Claims.Add((ClaimNames.TokenVersion, user.TokenVersion.ToString()));
        });

        return new AccessToken(token, expiresAt);
    }
}

public static class ClaimNames
{
    public const string UserId = "UserId";
    public const string Email = "Email";
    public const string TokenVersion = "TokenVersion";
}
