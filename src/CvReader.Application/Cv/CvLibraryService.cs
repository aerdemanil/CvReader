using CvReader.Application.Abstractions;

namespace CvReader.Application.Cv;

public record CvSummaryDto(Guid Id, string FileName, int PageCount, DateTime CreatedAt, Guid? FolderId);

public record CvPageDto(int Total, List<CvSummaryDto> Items);

public record CvDetailDto(Guid Id, string FileName, int PageCount, DateTime CreatedAt, Guid? FolderId, string Text);

public class CvLibraryService
{
    private readonly IProfileRepository _profiles;
    private readonly IFolderRepository _folders;

    public CvLibraryService(IProfileRepository profiles, IFolderRepository folders)
    {
        _profiles = profiles;
        _folders = folders;
    }

    public async Task<CvPageDto> GetPageAsync(Guid ownerId, ProfileFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var profiles = await _profiles.GetPageAsync(ownerId, filter, (page - 1) * pageSize, pageSize, ct);

        var items = profiles.Items
            .Select(p => new CvSummaryDto(p.Id, p.FileName, p.PageCount, p.CreatedAt, p.FolderId))
            .ToList();

        return new CvPageDto(profiles.Total, items);
    }

    // CV bulunamazsa null döner.
    public async Task<CvDetailDto?> GetAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var profile = await _profiles.GetByIdAsync(ownerId, id, ct);

        return profile is null
            ? null
            : new CvDetailDto(profile.Id, profile.FileName, profile.PageCount, profile.CreatedAt, profile.FolderId, profile.RawText);
    }

    // folderId null ise CV'ler klasörden çıkarılır. Hedef klasör bulunamazsa false döner.
    public async Task<bool> MoveAsync(Guid ownerId, IReadOnlyCollection<Guid> profileIds, Guid? folderId, CancellationToken ct)
    {
        if (folderId is not null && !await _folders.ExistsAsync(ownerId, folderId.Value, ct))
            return false;

        await _profiles.MoveAsync(ownerId, profileIds, folderId, ct);
        return true;
    }

    // CV bulunamazsa false döner.
    public Task<bool> DeleteAsync(Guid ownerId, Guid profileId, CancellationToken ct) =>
        _profiles.DeleteAsync(ownerId, profileId, ct);
}
