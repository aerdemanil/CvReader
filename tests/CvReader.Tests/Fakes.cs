using CvReader.Application.Abstractions;
using CvReader.Application.Auth;
using CvReader.Application.Cv;
using CvReader.Application.Embeddings;
using CvReader.Application.Folders;
using CvReader.Domain.Entities;

namespace CvReader.Tests;

// Servis testleri veritabanı ve Ollama olmadan çalışsın diye bellekte tutulan sahte bağımlılıklar.

public class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        Task.FromResult(Users.Any(u => u.Email == email));

    public Task AddAsync(User user, CancellationToken ct)
    {
        if (Users.Any(u => u.Email == user.Email))
            throw new EmailAlreadyExistsException(user.Email);

        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<int?> GetTokenVersionAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id)?.TokenVersion);

    public Task IncrementTokenVersionAsync(Guid id, CancellationToken ct)
    {
        Users.Single(u => u.Id == id).TokenVersion++;
        return Task.CompletedTask;
    }
}

public class FakePasswordHasher : IPasswordHasher
{
    public int HashCalls { get; private set; }

    public string Hash(string password)
    {
        HashCalls++;
        return $"hashed:{password}";
    }

    public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
}

public class FakeTokenService : ITokenService
{
    public AccessToken CreateToken(User user) =>
        new($"token:{user.Id}:{user.TokenVersion}", new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}

public class FakeEmbeddingService : IEmbeddingService
{
    public List<string> Inputs { get; } = [];
    public float[] Result { get; set; } = [1, 0];
    public bool Fail { get; set; }

    public Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        Inputs.Add(text);
        if (Fail) throw new EmbeddingException("The embedding service is not reachable.");
        return Task.FromResult(Result);
    }
}

public class FakeCvParser : ICvParser
{
    public bool Fail { get; set; }

    public ParsedCv Parse(Stream stream)
    {
        if (Fail) throw new InvalidCvFileException("The file could not be read as a valid PDF.");
        return new ParsedCv("parsed text", 2);
    }
}

public class FakeJobPostingRepository : IJobPostingRepository
{
    public List<(JobPosting Job, float[] Embedding)> Jobs { get; } = [];

    public Task AddAsync(JobPosting job, float[] embedding, CancellationToken ct)
    {
        Jobs.Add((job, embedding));
        return Task.CompletedTask;
    }

    public Task<JobPosting?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult<JobPosting?>(Owned(ownerId).FirstOrDefault(j => j.Job.Id == id).Job);

    public Task<List<JobPosting>> GetAllAsync(Guid ownerId, CancellationToken ct) =>
        Task.FromResult(Owned(ownerId).Select(j => j.Job).ToList());

    public Task<float[]?> GetEmbeddingAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult<float[]?>(Owned(ownerId).FirstOrDefault(j => j.Job.Id == id).Embedding);

    public Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult(Jobs.RemoveAll(j => j.Job.Id == id && j.Job.OwnerId == ownerId) > 0);

    private IEnumerable<(JobPosting Job, float[] Embedding)> Owned(Guid ownerId) =>
        Jobs.Where(j => j.Job.OwnerId == ownerId);
}

public class FakeProfileRepository : IProfileRepository
{
    public List<(Profile Profile, float[] Embedding)> Profiles { get; } = [];

    public Task AddAsync(Profile profile, float[] embedding, CancellationToken ct)
    {
        Profiles.Add((profile, embedding));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid ownerId, string contentHash, CancellationToken ct) =>
        Task.FromResult(Profiles.Any(p => p.Profile.OwnerId == ownerId && p.Profile.ContentHash == contentHash));

    public Task<RankedProfiles> GetRankedAsync(Guid ownerId, float[] query, int skip, int take, CancellationToken ct)
    {
        var owned = Profiles.Where(p => p.Profile.OwnerId == ownerId).ToList();

        var items = owned
            .Select(p => new RankedProfile(p.Profile.Id, p.Profile.FileName, Cosine(p.Embedding, query)))
            .OrderByDescending(p => p.Similarity)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult(new RankedProfiles(owned.Count, items));
    }

    public Task<ProfileSummaries> GetPageAsync(Guid ownerId, ProfileFilter filter, int skip, int take, CancellationToken ct)
    {
        var matching = Profiles
            .Select(p => p.Profile)
            .Where(p => p.OwnerId == ownerId)
            .Where(p => filter.Unfiled ? p.FolderId is null : filter.FolderId is null || p.FolderId == filter.FolderId)
            .Where(p => string.IsNullOrWhiteSpace(filter.Search) || p.FileName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var items = matching
            .Skip(skip)
            .Take(take)
            .Select(p => new ProfileSummary(p.Id, p.FileName, p.PageCount, p.CreatedAt, p.FolderId))
            .ToList();

        return Task.FromResult(new ProfileSummaries(matching.Count, items));
    }

    public Task<Profile?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult<Profile?>(Profiles.FirstOrDefault(p => p.Profile.Id == id && p.Profile.OwnerId == ownerId).Profile);

    public Task MoveAsync(Guid ownerId, IReadOnlyCollection<Guid> ids, Guid? folderId, CancellationToken ct)
    {
        foreach (var (profile, _) in Profiles.Where(p => p.Profile.OwnerId == ownerId && ids.Contains(p.Profile.Id)))
            profile.FolderId = folderId;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult(Profiles.RemoveAll(p => p.Profile.Id == id && p.Profile.OwnerId == ownerId) > 0);

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

public class FakeFolderRepository : IFolderRepository
{
    private readonly FakeProfileRepository _profiles;

    public FakeFolderRepository(FakeProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public List<Folder> Folders { get; } = [];

    public Task AddAsync(Folder folder, CancellationToken ct)
    {
        if (Folders.Any(f => f.OwnerId == folder.OwnerId && f.Name == folder.Name))
            throw new FolderNameExistsException(folder.Name);

        Folders.Add(folder);
        return Task.CompletedTask;
    }

    public Task<List<FolderWithCount>> GetAllAsync(Guid ownerId, CancellationToken ct) =>
        Task.FromResult(Folders
            .Where(f => f.OwnerId == ownerId)
            .Select(f => new FolderWithCount(f, _profiles.Profiles.Count(p => p.Profile.FolderId == f.Id)))
            .ToList());

    public Task<int> CountUnfiledCvsAsync(Guid ownerId, CancellationToken ct) =>
        Task.FromResult(_profiles.Profiles.Count(p => p.Profile.OwnerId == ownerId && p.Profile.FolderId is null));

    public Task<bool> ExistsAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult(Folders.Any(f => f.Id == id && f.OwnerId == ownerId));

    public Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var removed = Folders.RemoveAll(f => f.Id == id && f.OwnerId == ownerId) > 0;
        if (removed)
        {
            foreach (var (profile, _) in _profiles.Profiles.Where(p => p.Profile.FolderId == id))
                profile.FolderId = null;
        }
        return Task.FromResult(removed);
    }
}
