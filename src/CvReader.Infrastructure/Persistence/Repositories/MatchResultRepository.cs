using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence.Repositories;

public class MatchResultRepository : IMatchResultRepository
{
    private readonly AppDbContext _db;

    public MatchResultRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task ReplaceForJobAsync(Guid jobPostingId, List<MatchResult> results, CancellationToken ct)
    {
        // Eski sonuçları silme ve yenilerini ekleme ya birlikte başarılı olur ya da hiçbiri olmaz.
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.MatchResults
            .Where(r => r.JobPostingId == jobPostingId)
            .ExecuteDeleteAsync(ct);

        _db.MatchResults.AddRange(results);
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
    }

    public Task<List<MatchResult>> GetByJobAsync(Guid jobPostingId, CancellationToken ct) =>
        _db.MatchResults
            .AsNoTracking()
            .Include(r => r.Profile)
            .Where(r => r.JobPostingId == jobPostingId)
            .OrderByDescending(r => r.Score)
            .ToListAsync(ct);
}
