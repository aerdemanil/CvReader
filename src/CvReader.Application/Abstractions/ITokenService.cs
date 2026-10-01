using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
