using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Auth;

public record AuthResult(string Email, string Token, DateTime ExpiresAt);

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
            Id = Guid.CreateVersion7(),
            Email = normalizedEmail,
            PasswordHash = _hasher.Hash(password)
        };

        await _users.AddAsync(user, ct);
        return ToResult(user);
    }

    public async Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(email), ct);

        if (user is null)
        {
            // Kullanıcı yokken de aynı hash maliyeti ödenir; yanıt süresi e-postanın kayıtlı olup olmadığını ele vermez.
            _hasher.Hash(password);
            return null;
        }

        return _hasher.Verify(password, user.PasswordHash) ? ToResult(user) : null;
    }

    public Task LogoutAsync(Guid userId, CancellationToken ct) =>
        _users.IncrementTokenVersionAsync(userId, ct);

    // Çıkış yapılmış ya da kullanıcısı silinmiş bir token'ı reddeder.
    public async Task<bool> IsSessionValidAsync(Guid userId, int tokenVersion, CancellationToken ct) =>
        await _users.GetTokenVersionAsync(userId, ct) == tokenVersion;

    private AuthResult ToResult(User user)
    {
        var token = _tokens.CreateToken(user);
        return new AuthResult(user.Email, token.Token, token.ExpiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
