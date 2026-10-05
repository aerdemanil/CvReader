using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public record RankedProfile(Guid ProfileId, string FileName, double Similarity);

public record RankedProfiles(int Total, List<RankedProfile> Items);

public record ProfileSummary(Guid Id, string FileName, int PageCount, DateTime CreatedAt, Guid? FolderId);

public record ProfileSummaries(int Total, List<ProfileSummary> Items);

// Unfiled: yalnızca klasörsüz CV'ler. FolderId ve Unfiled boşsa tüm CV'ler listelenir.
public record ProfileFilter(Guid? FolderId, bool Unfiled, string? Search);

// Tüm okuma ve silme işlemleri sahibine göre filtrelenir; başkasının CV'si "yok" sayılır.
public interface IProfileRepository
{
    Task AddAsync(Profile profile, float[] embedding, CancellationToken ct);
    Task<bool> ExistsAsync(Guid ownerId, string contentHash, CancellationToken ct);

    // Sahibin profilleri, sorgu vektörüne cosine benzerliğine göre azalan sırada.
    Task<RankedProfiles> GetRankedAsync(Guid ownerId, float[] query, int skip, int take, CancellationToken ct);

    // Yeniden eskiye sıralı; CV metni içermez.
    Task<ProfileSummaries> GetPageAsync(Guid ownerId, ProfileFilter filter, int skip, int take, CancellationToken ct);
    Task<Profile?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct);

    // folderId null ise CV'ler klasörden çıkarılır.
    Task MoveAsync(Guid ownerId, IReadOnlyCollection<Guid> ids, Guid? folderId, CancellationToken ct);
    Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct);
}
