using CvReader.Application.Abstractions;
using CvReader.Application.Cv;
using CvReader.Application.Folders;
using CvReader.Domain.Entities;

namespace CvReader.Tests;

public class CvLibraryAndFolderTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();
    private static readonly ProfileFilter All = new(null, false, null);

    private readonly FakeProfileRepository _profiles = new();
    private readonly FakeFolderRepository _folderRepository;
    private readonly FolderService _folders;
    private readonly CvLibraryService _library;

    public CvLibraryAndFolderTests()
    {
        _folderRepository = new FakeFolderRepository(_profiles);
        _folders = new FolderService(_folderRepository);
        _library = new CvLibraryService(_profiles, _folderRepository);
    }

    private Guid AddCv(Guid ownerId, string fileName, Guid? folderId = null)
    {
        var profile = new Profile { Id = Guid.NewGuid(), OwnerId = ownerId, FolderId = folderId, FileName = fileName, RawText = $"text of {fileName}" };
        _profiles.Profiles.Add((profile, [1, 0]));
        return profile.Id;
    }

    [Fact]
    public async Task Folder_name_is_trimmed_and_must_be_unique_per_user()
    {
        var folder = await _folders.CreateAsync(Owner, "  Backend  ", CancellationToken.None);

        Assert.Equal("Backend", folder.Name);
        await Assert.ThrowsAsync<FolderNameExistsException>(() => _folders.CreateAsync(Owner, "Backend", CancellationToken.None));

        // Başka bir kullanıcı aynı adı kullanabilir.
        await _folders.CreateAsync(Stranger, "Backend", CancellationToken.None);
    }

    [Fact]
    public async Task Folder_list_shows_only_own_folders_with_cv_counts()
    {
        var backend = await _folders.CreateAsync(Owner, "Backend", CancellationToken.None);
        await _folders.CreateAsync(Stranger, "Not mine", CancellationToken.None);
        AddCv(Owner, "a.pdf", backend.Id);
        AddCv(Owner, "b.pdf", backend.Id);
        AddCv(Owner, "c.pdf");

        var list = await _folders.GetAllAsync(Owner, CancellationToken.None);

        var only = Assert.Single(list.Folders);
        Assert.Equal("Backend", only.Name);
        Assert.Equal(2, only.CvCount);
        Assert.Equal(1, list.UnfiledCount);
    }

    [Fact]
    public async Task Deleting_a_folder_keeps_its_cvs_as_unfiled()
    {
        var backend = await _folders.CreateAsync(Owner, "Backend", CancellationToken.None);
        AddCv(Owner, "a.pdf", backend.Id);

        Assert.False(await _folders.DeleteAsync(Stranger, backend.Id, CancellationToken.None));
        Assert.True(await _folders.DeleteAsync(Owner, backend.Id, CancellationToken.None));

        var page = await _library.GetPageAsync(Owner, All, 1, 50, CancellationToken.None);
        Assert.Null(Assert.Single(page.Items).FolderId);
    }

    [Fact]
    public async Task Cv_list_is_filtered_by_owner_folder_and_search()
    {
        var backend = await _folders.CreateAsync(Owner, "Backend", CancellationToken.None);
        AddCv(Owner, "ada-backend.pdf", backend.Id);
        AddCv(Owner, "grace.pdf");
        AddCv(Stranger, "not-mine.pdf");

        var all = await _library.GetPageAsync(Owner, All, 1, 50, CancellationToken.None);
        var inFolder = await _library.GetPageAsync(Owner, new ProfileFilter(backend.Id, false, null), 1, 50, CancellationToken.None);
        var unfiled = await _library.GetPageAsync(Owner, new ProfileFilter(null, true, null), 1, 50, CancellationToken.None);
        var searched = await _library.GetPageAsync(Owner, new ProfileFilter(null, false, "GRACE"), 1, 50, CancellationToken.None);

        Assert.Equal(2, all.Total);
        Assert.Equal("ada-backend.pdf", Assert.Single(inFolder.Items).FileName);
        Assert.Equal("grace.pdf", Assert.Single(unfiled.Items).FileName);
        Assert.Equal("grace.pdf", Assert.Single(searched.Items).FileName);
    }

    [Fact]
    public async Task Cv_text_is_visible_only_to_its_owner()
    {
        var id = AddCv(Owner, "ada.pdf");

        Assert.Equal("text of ada.pdf", (await _library.GetAsync(Owner, id, CancellationToken.None))?.Text);
        Assert.Null(await _library.GetAsync(Stranger, id, CancellationToken.None));
    }

    [Fact]
    public async Task Cvs_can_be_moved_into_and_out_of_own_folders()
    {
        var backend = await _folders.CreateAsync(Owner, "Backend", CancellationToken.None);
        var id = AddCv(Owner, "ada.pdf");

        Assert.True(await _library.MoveAsync(Owner, [id], backend.Id, CancellationToken.None));
        Assert.Equal(backend.Id, (await _library.GetAsync(Owner, id, CancellationToken.None))?.FolderId);

        Assert.True(await _library.MoveAsync(Owner, [id], null, CancellationToken.None));
        Assert.Null((await _library.GetAsync(Owner, id, CancellationToken.None))?.FolderId);
    }

    [Fact]
    public async Task Cvs_cannot_be_moved_into_another_users_folder()
    {
        var theirs = await _folders.CreateAsync(Stranger, "Theirs", CancellationToken.None);
        var id = AddCv(Owner, "ada.pdf");

        Assert.False(await _library.MoveAsync(Owner, [id], theirs.Id, CancellationToken.None));
        Assert.Null((await _library.GetAsync(Owner, id, CancellationToken.None))?.FolderId);
    }

    [Fact]
    public async Task Another_users_cv_is_not_moved_or_deleted()
    {
        var mine = await _folders.CreateAsync(Stranger, "Mine", CancellationToken.None);
        var id = AddCv(Owner, "ada.pdf");

        await _library.MoveAsync(Stranger, [id], mine.Id, CancellationToken.None);
        Assert.Null((await _library.GetAsync(Owner, id, CancellationToken.None))?.FolderId);

        Assert.False(await _library.DeleteAsync(Stranger, id, CancellationToken.None));
        Assert.True(await _library.DeleteAsync(Owner, id, CancellationToken.None));
    }
}
