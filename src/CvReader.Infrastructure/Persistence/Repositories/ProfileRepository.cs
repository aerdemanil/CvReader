using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly AppDbContext _db;

    public ProfileRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Profile profile, CancellationToken ct)
    {
        _db.Profiles.Add(profile);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Profile>> GetAllAsync(CancellationToken ct) =>
        _db.Profiles.AsNoTracking().ToListAsync(ct);
}
