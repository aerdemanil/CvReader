using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Folders;

public record FolderDto(Guid Id, string Name, int CvCount);

public record FolderListDto(List<FolderDto> Folders, int UnfiledCount);

public class FolderService
{
    private readonly IFolderRepository _folders;

    public FolderService(IFolderRepository folders)
    {
        _folders = folders;
    }

    public async Task<FolderDto> CreateAsync(Guid ownerId, string name, CancellationToken ct)
    {
        var folder = new Folder
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Name = name.Trim()
        };

        await _folders.AddAsync(folder, ct);
        return new FolderDto(folder.Id, folder.Name, 0);
    }

    public async Task<FolderListDto> GetAllAsync(Guid ownerId, CancellationToken ct)
    {
        var folders = await _folders.GetAllAsync(ownerId, ct);
        var unfiledCount = await _folders.CountUnfiledCvsAsync(ownerId, ct);

        return new FolderListDto(
            folders.Select(f => new FolderDto(f.Folder.Id, f.Folder.Name, f.CvCount)).ToList(),
            unfiledCount);
    }

    public Task<bool> ExistsAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        _folders.ExistsAsync(ownerId, id, ct);

    // Klasör bulunamazsa false döner.
    public Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        _folders.DeleteAsync(ownerId, id, ct);
}
