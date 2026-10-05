using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace CvReader.Infrastructure.Persistence.Repositories;

public class JobPostingRepository : IJobPostingRepository
{
    private readonly AppDbContext _db;

    public JobPostingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(JobPosting job, IReadOnlyList<TermEmbedding> terms, CancellationToken ct)
    {
        _db.JobPostings.Add(job);
        _db.JobPostingTerms.AddRange(terms.Select(t => new JobPostingTerm
        {
            JobPostingId = job.Id,
            Term = t.Term,
            Embedding = new Vector(t.Embedding)
        }));
        await _db.SaveChangesAsync(ct);
    }

    public Task<JobPosting?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        _db.JobPostings.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id && j.OwnerId == ownerId, ct);

    public Task<List<JobPosting>> GetAllAsync(Guid ownerId, CancellationToken ct) =>
        _db.JobPostings
            .AsNoTracking()
            .Where(j => j.OwnerId == ownerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    // Terim satırları veritabanındaki cascade ile birlikte silinir.
    public async Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        await _db.JobPostings
            .Where(j => j.Id == id && j.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct) > 0;
}
