using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

// Bir CV'nin terimleri içinde, ilanın JobTerm terimine en yakın olanın cosine benzerliği.
public record ProfileTermSimilarity(Guid ProfileId, string FileName, string JobTerm, double Similarity);

// İlanın JobTerm terimi ile CV'nin CvTerm terimi arasındaki cosine benzerliği.
public record TermMatch(string JobTerm, string CvTerm, double Similarity);

public record ProfileSummary(Guid Id, string FileName, int PageCount, DateTime CreatedAt, Guid? FolderId);

public record ProfileSummaries(int Total, List<ProfileSummary> Items);

// Unfiled: yalnızca klasörsüz CV'ler. FolderId ve Unfiled boşsa tüm CV'ler listelenir.
public record ProfileFilter(Guid? FolderId, bool Unfiled, string? Search);

// Tüm okuma ve silme işlemleri sahibine göre filtrelenir; başkasının CV'si "yok" sayılır.
public interface IProfileRepository
{
    Task AddAsync(Profile profile, IReadOnlyList<TermEmbedding> terms, CancellationToken ct);
    Task<bool> ExistsAsync(Guid ownerId, string contentHash, CancellationToken ct);

    // Sahibin her CV'si ve ilanın her terimi için bir satır.
    Task<List<ProfileTermSimilarity>> GetBestSimilaritiesAsync(Guid ownerId, Guid jobPostingId, CancellationToken ct);

    // Tek bir CV'nin, ilanın terimlerine en az minSimilarity kadar yakın olan terimleri.
    Task<List<TermMatch>> GetCloseTermsAsync(Guid ownerId, Guid jobPostingId, Guid profileId, double minSimilarity, CancellationToken ct);

    // Yeniden eskiye sıralı; CV metni içermez.
    Task<ProfileSummaries> GetPageAsync(Guid ownerId, ProfileFilter filter, int skip, int take, CancellationToken ct);
    Task<Profile?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct);

    // folderId null ise CV'ler klasörden çıkarılır.
    Task MoveAsync(Guid ownerId, IReadOnlyCollection<Guid> ids, Guid? folderId, CancellationToken ct);
    Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct);
}
