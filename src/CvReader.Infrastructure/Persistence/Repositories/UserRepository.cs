using CvReader.Application.Abstractions;
using CvReader.Application.Auth;
using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CvReader.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        // Aynı e-postayla eşzamanlı iki kayıt isteğinde ön kontrolü ikisi de geçer; benzersiz indeks ikincisini durdurur.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _db.Entry(user).State = EntityState.Detached;
            throw new EmailAlreadyExistsException(user.Email);
        }
    }

    public async Task<int?> GetTokenVersionAsync(Guid id, CancellationToken ct) =>
        await _db.Users
            .Where(u => u.Id == id)
            .Select(u => (int?)u.TokenVersion)
            .FirstOrDefaultAsync(ct);

    public Task IncrementTokenVersionAsync(Guid id, CancellationToken ct) =>
        _db.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1), ct);
}
