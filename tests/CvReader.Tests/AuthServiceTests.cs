using CvReader.Application.Auth;

namespace CvReader.Tests;

public class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_users, _hasher, new FakeTokenService());
    }

    [Fact]
    public async Task Register_normalizes_email_and_stores_hashed_password()
    {
        var result = await _service.RegisterAsync("  Ada@Example.COM ", "secret-password", CancellationToken.None);

        var user = Assert.Single(_users.Users);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Equal("ada@example.com", result.Email);
        Assert.NotEqual("secret-password", user.PasswordHash);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Register_with_existing_email_throws()
    {
        await _service.RegisterAsync("ada@example.com", "secret-password", CancellationToken.None);

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _service.RegisterAsync("ADA@example.com", "other-password", CancellationToken.None));
    }

    [Fact]
    public async Task Login_with_correct_password_returns_token()
    {
        await _service.RegisterAsync("ada@example.com", "secret-password", CancellationToken.None);

        var result = await _service.LoginAsync("Ada@example.com", "secret-password", CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_null()
    {
        await _service.RegisterAsync("ada@example.com", "secret-password", CancellationToken.None);

        Assert.Null(await _service.LoginAsync("ada@example.com", "wrong-password", CancellationToken.None));
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_null_but_still_pays_the_hash_cost()
    {
        var result = await _service.LoginAsync("nobody@example.com", "secret-password", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, _hasher.HashCalls);
    }

    [Fact]
    public async Task Logout_invalidates_tokens_issued_before_it()
    {
        await _service.RegisterAsync("ada@example.com", "secret-password", CancellationToken.None);
        var user = _users.Users[0];
        var versionInToken = user.TokenVersion;

        Assert.True(await _service.IsSessionValidAsync(user.Id, versionInToken, CancellationToken.None));

        await _service.LogoutAsync(user.Id, CancellationToken.None);

        Assert.False(await _service.IsSessionValidAsync(user.Id, versionInToken, CancellationToken.None));
    }

    [Fact]
    public async Task Session_of_a_deleted_user_is_invalid()
    {
        Assert.False(await _service.IsSessionValidAsync(Guid.NewGuid(), 0, CancellationToken.None));
    }
}
