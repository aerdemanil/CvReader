using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvReader.Infrastructure.Persistence.Repositories;

public class JobPostingRepository : IJobPostingRepository
{
    private readonly AppDbContext _db;

    public JobPostingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(JobPosting job, CancellationToken ct)
    {
        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync(ct);
    }

    public Task<JobPosting?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.JobPostings.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<List<JobPosting>> GetAllAsync(CancellationToken ct) =>
        _db.JobPostings.AsNoTracking().OrderByDescending(j => j.CreatedAt).ToListAsync(ct);
}
