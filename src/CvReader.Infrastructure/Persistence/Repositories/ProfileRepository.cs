using CvReader.Application.Abstractions;
using CvReader.Application.Cv;
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
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Kaydedilemeyen profil context'te kalırsa sonraki SaveChanges çağrıları da patlar.
            _db.Entry(profile).State = EntityState.Detached;
            throw new ProfileSaveException("The CV could not be saved.", ex);
        }
    }

    public Task<List<Profile>> GetAllAsync(CancellationToken ct) =>
        _db.Profiles.AsNoTracking().ToListAsync(ct);
}
