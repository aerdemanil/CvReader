using CvReader.Application.Abstractions;
using CvReader.Application.Cv;
using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly AppDbContext _db;

    public ProfileRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Profile profile, float[] embedding, CancellationToken ct)
    {
        var profileEmbedding = new ProfileEmbedding
        {
            ProfileId = profile.Id,
            Embedding = new Vector(embedding)
        };

        _db.Profiles.Add(profile);
        _db.ProfileEmbeddings.Add(profileEmbedding);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Kaydedilemeyen kayıtlar context'te kalırsa sonraki SaveChanges çağrıları da patlar.
            _db.Entry(profile).State = EntityState.Detached;
            _db.Entry(profileEmbedding).State = EntityState.Detached;
            throw new ProfileSaveException("The CV could not be saved.", ex);
        }
    }

    public Task<Dictionary<Guid, double>> GetSimilaritiesAsync(float[] query, CancellationToken ct)
    {
        var queryVector = new Vector(query);

        // CosineDistance, pgvector'ün <=> operatörüne çevrilir; benzerlik = 1 - uzaklık.
        return _db.ProfileEmbeddings
            .Select(e => new { e.ProfileId, Distance = e.Embedding.CosineDistance(queryVector) })
            .ToDictionaryAsync(x => x.ProfileId, x => 1 - x.Distance, ct);
    }
}
