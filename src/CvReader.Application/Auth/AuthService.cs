using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Auth;

public record AuthResult(Guid UserId, string Email, string Role, string Token, DateTime ExpiresAt);

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (await _users.EmailExistsAsync(normalizedEmail, ct))
            throw new EmailAlreadyExistsException(normalizedEmail);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _hasher.Hash(password),
            Role = UserRoles.Employer
        };

        await _users.AddAsync(user, ct);
        return ToResult(user);
    }

    public async Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(email), ct);

        if (user is null || !_hasher.Verify(password, user.PasswordHash))
            return null;

        return ToResult(user);
    }

    private AuthResult ToResult(User user)
    {
        var token = _tokens.CreateToken(user);
        return new AuthResult(user.Id, user.Email, user.Role, token.Token, token.ExpiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
