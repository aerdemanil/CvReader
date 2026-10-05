using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    // E-posta zaten kayıtlıysa EmailAlreadyExistsException fırlatır.
    Task AddAsync(User user, CancellationToken ct);

    // Kullanıcı yoksa null döner.
    Task<int?> GetTokenVersionAsync(Guid id, CancellationToken ct);
    Task IncrementTokenVersionAsync(Guid id, CancellationToken ct);
}
