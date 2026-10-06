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

    public async Task AddAsync(Profile profile, IReadOnlyList<TermEmbedding> terms, CancellationToken ct)
    {
        var profileTerms = terms
            .Select(t => new ProfileTerm { ProfileId = profile.Id, Term = t.Term, Embedding = new Vector(t.Embedding) })
            .ToList();

        _db.Profiles.Add(profile);
        _db.ProfileTerms.AddRange(profileTerms);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Kaydedilemeyen kayıtlar context'te kalırsa sonraki SaveChanges çağrıları da patlar.
            _db.Entry(profile).State = EntityState.Detached;
            foreach (var term in profileTerms)
                _db.Entry(term).State = EntityState.Detached;
            throw new ProfileSaveException("The CV could not be saved.", ex);
        }
    }

    public Task<bool> ExistsAsync(Guid ownerId, string contentHash, CancellationToken ct) =>
        _db.Profiles.AnyAsync(p => p.OwnerId == ownerId && p.ContentHash == contentHash, ct);

    public async Task<List<ProfileTermSimilarity>> GetBestSimilaritiesAsync(Guid ownerId, Guid jobPostingId, CancellationToken ct)
    {
        // CosineDistance, pgvector'ün <=> operatörüne çevrilir; benzerlik = 1 - uzaklık.
        // Sadece listede gösterilen alanlar seçilir; CV metni (RawText) çekilmez.
        var rows = await (
            from profile in _db.Profiles
            where profile.OwnerId == ownerId
            from job in _db.JobPostings
            where job.Id == jobPostingId && job.OwnerId == ownerId
            join jobTerm in _db.JobPostingTerms on job.Id equals jobTerm.JobPostingId
            select new
            {
                profile.Id,
                profile.FileName,
                profile.FullName,
                jobTerm.Term,
                Distance = _db.ProfileTerms
                    .Where(t => t.ProfileId == profile.Id)
                    .Min(t => (double?)t.Embedding.CosineDistance(jobTerm.Embedding))
            }).ToListAsync(ct);

        return rows.Select(x => new ProfileTermSimilarity(x.Id, x.FileName, x.FullName, x.Term, 1 - (x.Distance ?? 1))).ToList();
    }

    public async Task<List<TermMatch>> GetCloseTermsAsync(Guid ownerId, Guid jobPostingId, Guid profileId, double minSimilarity, CancellationToken ct)
    {
        var maxDistance = 1 - minSimilarity;

        var rows = await (
            from profile in _db.Profiles
            where profile.Id == profileId && profile.OwnerId == ownerId
            from job in _db.JobPostings
            where job.Id == jobPostingId && job.OwnerId == ownerId
            join jobTerm in _db.JobPostingTerms on job.Id equals jobTerm.JobPostingId
            join profileTerm in _db.ProfileTerms on profile.Id equals profileTerm.ProfileId
            let distance = profileTerm.Embedding.CosineDistance(jobTerm.Embedding)
            where distance <= maxDistance
            select new { JobTerm = jobTerm.Term, CvTerm = profileTerm.Term, Distance = distance }).ToListAsync(ct);

        return rows.Select(x => new TermMatch(x.JobTerm, x.CvTerm, 1 - x.Distance)).ToList();
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
            var pattern = $"%{escaped}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.FileName, pattern, @"\")
                || EF.Functions.ILike(p.FullName!, pattern, @"\")
                || EF.Functions.ILike(p.Email!, pattern, @"\"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProfileSummary(p.Id, p.FileName, p.PageCount, p.CreatedAt, p.FolderId, p.FullName, p.Email, p.Phone))
            .ToListAsync(ct);

        return new ProfileSummaries(total, items);
    }

    public Task<Profile?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

    public Task MoveAsync(Guid ownerId, IReadOnlyCollection<Guid> ids, Guid? folderId, CancellationToken ct) =>
        _db.Profiles
            .Where(p => p.OwnerId == ownerId && ids.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.FolderId, folderId), ct);

    // Terim satırları veritabanındaki cascade ile birlikte silinir.
    public async Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        await _db.Profiles
            .Where(p => p.Id == id && p.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct) > 0;

    public Task<List<PendingExtraction>> GetPendingExtractionAsync(int take, CancellationToken ct) =>
        _db.Profiles
            .AsNoTracking()
            .Where(p => p.FieldsExtractedAt == null)
            .OrderBy(p => p.CreatedAt)
            .Take(take)
            .Select(p => new PendingExtraction(p.Id, p.RawText))
            .ToListAsync(ct);

    public Task SaveExtractedFieldsAsync(Guid id, ExtractedCvFields fields, DateTime extractedAt, CancellationToken ct) =>
        _db.Profiles
            .Where(p => p.Id == id && p.FieldsExtractedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.FullName, fields.FullName)
                .SetProperty(p => p.Email, fields.Email)
                .SetProperty(p => p.Phone, fields.Phone)
                .SetProperty(p => p.FieldsExtractedAt, extractedAt), ct);
}
