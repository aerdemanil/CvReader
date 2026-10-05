using CvReader.Application.Abstractions;
using CvReader.Application.Folders;
using CvReader.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CvReader.Infrastructure.Persistence.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly AppDbContext _db;

    public FolderRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Folder folder, CancellationToken ct)
    {
        _db.Folders.Add(folder);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _db.Entry(folder).State = EntityState.Detached;
            throw new FolderNameExistsException(folder.Name);
        }
    }

    public async Task<List<FolderWithCount>> GetAllAsync(Guid ownerId, CancellationToken ct)
    {
        var rows = await _db.Folders
            .AsNoTracking()
            .Where(f => f.OwnerId == ownerId)
            .OrderBy(f => f.Name)
            .Select(f => new { Folder = f, CvCount = _db.Profiles.Count(p => p.FolderId == f.Id) })
            .ToListAsync(ct);

        return rows.Select(r => new FolderWithCount(r.Folder, r.CvCount)).ToList();
    }

    public Task<int> CountUnfiledCvsAsync(Guid ownerId, CancellationToken ct) =>
        _db.Profiles.CountAsync(p => p.OwnerId == ownerId && p.FolderId == null, ct);

    public Task<bool> ExistsAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        _db.Folders.AnyAsync(f => f.Id == id && f.OwnerId == ownerId, ct);

    // CV'lerin FolderId alanı veritabanındaki SET NULL ile boşaltılır.
    public async Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        await _db.Folders
            .Where(f => f.Id == id && f.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct) > 0;
}
