using CvReader.Domain.Entities;

namespace CvReader.Application.Abstractions;

public record FolderWithCount(Folder Folder, int CvCount);

// Tüm okuma ve silme işlemleri sahibine göre filtrelenir; başkasının klasörü "yok" sayılır.
public interface IFolderRepository
{
    // Aynı adla klasör varsa FolderNameExistsException fırlatır.
    Task AddAsync(Folder folder, CancellationToken ct);
    Task<List<FolderWithCount>> GetAllAsync(Guid ownerId, CancellationToken ct);
    Task<int> CountUnfiledCvsAsync(Guid ownerId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid ownerId, Guid id, CancellationToken ct);

    // İçindeki CV'ler silinmez, klasörsüz kalır.
    Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct);
}
