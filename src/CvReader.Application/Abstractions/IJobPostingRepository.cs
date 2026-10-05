using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

// Tüm okuma ve silme işlemleri sahibine göre filtrelenir; başkasının ilanı "yok" sayılır.
public interface IJobPostingRepository
{
    Task AddAsync(JobPosting job, float[] embedding, CancellationToken ct);
    Task<JobPosting?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct);
    Task<List<JobPosting>> GetAllAsync(Guid ownerId, CancellationToken ct);
    Task<float[]?> GetEmbeddingAsync(Guid ownerId, Guid id, CancellationToken ct);
    Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct);
}
