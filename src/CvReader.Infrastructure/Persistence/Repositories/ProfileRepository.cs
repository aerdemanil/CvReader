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

    public Task<bool> ExistsAsync(Guid ownerId, string contentHash, CancellationToken ct) =>
        _db.Profiles.AnyAsync(p => p.OwnerId == ownerId && p.ContentHash == contentHash, ct);

    public async Task<RankedProfiles> GetRankedAsync(Guid ownerId, float[] query, int skip, int take, CancellationToken ct)
    {
        var queryVector = new Vector(query);

        var total = await _db.Profiles.CountAsync(p => p.OwnerId == ownerId, ct);

        // CosineDistance, pgvector'ün <=> operatörüne çevrilir; benzerlik = 1 - uzaklık.
        // Sadece listede gösterilen alanlar seçilir; CV metni (RawText) çekilmez.
        var rows = await _db.Profiles
            .Where(p => p.OwnerId == ownerId)
            .Join(_db.ProfileEmbeddings, p => p.Id, e => e.ProfileId,
                (p, e) => new { p.Id, p.FileName, Distance = e.Embedding.CosineDistance(queryVector) })
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Id) // eşit skorlarda sayfalar arası sıra sabit kalsın
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var items = rows.Select(x => new RankedProfile(x.Id, x.FileName, 1 - x.Distance)).ToList();
        return new RankedProfiles(total, items);
    }

    public async Task<ProfileSummaries> GetPageAsync(Guid ownerId, ProfileFilter filter, int skip, int take, CancellationToken ct)
    {
        var query = _db.Profiles.AsNoTracking().Where(p => p.OwnerId == ownerId);

        if (filter.Unfiled)
            query = query.Where(p => p.FolderId == null);
        else if (filter.FolderId is not null)
            query = query.Where(p => p.FolderId == filter.FolderId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // Kullanıcının yazdığı % ve _ karakterleri joker değil, düz metin olarak aranır.
            var escaped = filter.Search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
            // Npgsql kaçış karakteri açıkça verilmezse ILIKE'ı kaçışsız (ESCAPE '') üretir.
            query = query.Where(p => EF.Functions.ILike(p.FileName, $"%{escaped}%", @"\"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProfileSummary(p.Id, p.FileName, p.PageCount, p.CreatedAt, p.FolderId))
            .ToListAsync(ct);

        return new ProfileSummaries(total, items);
    }

    public Task<Profile?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

    public Task MoveAsync(Guid ownerId, IReadOnlyCollection<Guid> ids, Guid? folderId, CancellationToken ct) =>
        _db.Profiles
            .Where(p => p.OwnerId == ownerId && ids.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.FolderId, folderId), ct);

    // Vektör satırı veritabanındaki cascade ile birlikte silinir.
    public async Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        await _db.Profiles
            .Where(p => p.Id == id && p.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct) > 0;
}
