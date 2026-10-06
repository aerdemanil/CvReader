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
    public List<IReadOnlyList<string>> Inputs { get; } = [];
    public float[] Result { get; set; } = [1, 0];
    public bool Fail { get; set; }

    public Task<List<TermEmbedding>> EmbedAsync(IReadOnlyList<string> terms, CancellationToken ct)
    {
        Inputs.Add(terms);
        if (Fail) throw new EmbeddingException("The embedding service is not reachable.");
        return Task.FromResult(terms.Select(t => new TermEmbedding(t, Result)).ToList());
    }
}

public class FakeCvParser : ICvParser
{
    public bool Fail { get; set; }
    public string Text { get; set; } = "parsed text";

    public ParsedCv Parse(Stream stream)
    {
        if (Fail) throw new InvalidCvFileException("The file could not be read as a valid PDF.");
        return new ParsedCv(Text, 2);
    }
}

public class FakeCvNameExtractor : ICvNameExtractor
{
    public List<string> Inputs { get; } = [];
    public string? Result { get; set; }
    public bool Fail { get; set; }

    public Task<string?> ExtractNameAsync(string cvHead, CancellationToken ct)
    {
        Inputs.Add(cvHead);
        if (Fail) throw new CvNameExtractionException("The name extraction service is not reachable.");
        return Task.FromResult(Result);
    }
}

public class FakeJobPostingRepository : IJobPostingRepository
{
    public List<(JobPosting Job, IReadOnlyList<TermEmbedding> Terms)> Jobs { get; } = [];

    public Task AddAsync(JobPosting job, IReadOnlyList<TermEmbedding> terms, CancellationToken ct)
    {
        Jobs.Add((job, terms));
        return Task.CompletedTask;
    }

    public Task<JobPosting?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult<JobPosting?>(Owned(ownerId).FirstOrDefault(j => j.Job.Id == id).Job);

    public Task<List<JobPosting>> GetAllAsync(Guid ownerId, CancellationToken ct) =>
        Task.FromResult(Owned(ownerId).Select(j => j.Job).ToList());

    public Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        Task.FromResult(Jobs.RemoveAll(j => j.Job.Id == id && j.Job.OwnerId == ownerId) > 0);

    private IEnumerable<(JobPosting Job, IReadOnlyList<TermEmbedding> Terms)> Owned(Guid ownerId) =>
        Jobs.Where(j => j.Job.OwnerId == ownerId);
}

public class FakeProfileRepository : IProfileRepository
{
    private readonly FakeJobPostingRepository _jobs;

    public FakeProfileRepository(FakeJobPostingRepository jobs)
    {
        _jobs = jobs;
    }

    public List<(Profile Profile, IReadOnlyList<TermEmbedding> Terms)> Profiles { get; } = [];

    public Task AddAsync(Profile profile, IReadOnlyList<TermEmbedding> terms, CancellationToken ct)
    {
        Profiles.Add((profile, terms));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid ownerId, string contentHash, CancellationToken ct) =>
        Task.FromResult(Profiles.Any(p => p.Profile.OwnerId == ownerId && p.Profile.ContentHash == contentHash));

    public Task<List<ProfileTermSimilarity>> GetBestSimilaritiesAsync(Guid ownerId, Guid jobPostingId, CancellationToken ct) =>
        Task.FromResult((
            from p in Profiles
            where p.Profile.OwnerId == ownerId
            from jobTerm in JobTerms(ownerId, jobPostingId)
            select new ProfileTermSimilarity(
                p.Profile.Id,
                p.Profile.FileName,
                p.Profile.FullName,
                jobTerm.Term,
                p.Terms.Max(t => Cosine(t.Embedding, jobTerm.Embedding)))).ToList());

    public Task<List<TermMatch>> GetCloseTermsAsync(Guid ownerId, Guid jobPostingId, Guid profileId, double minSimilarity, CancellationToken ct) =>
        Task.FromResult((
            from p in Profiles
            where p.Profile.Id == profileId && p.Profile.OwnerId == ownerId
            from jobTerm in JobTerms(ownerId, jobPostingId)
            from cvTerm in p.Terms
            let similarity = Cosine(cvTerm.Embedding, jobTerm.Embedding)
            where similarity >= minSimilarity
            select new TermMatch(jobTerm.Term, cvTerm.Term, similarity)).ToList());

    public Task<ProfileSummaries> GetPageAsync(Guid ownerId, ProfileFilter filter, int skip, int take, CancellationToken ct)
    {
        var matching = Profiles
            .Select(p => p.Profile)
            .Where(p => p.OwnerId == ownerId)
            .Where(p => filter.Unfiled ? p.FolderId is null : filter.FolderId is null || p.FolderId == filter.FolderId)
            .Where(p => string.IsNullOrWhiteSpace(filter.Search)
                || new[] { p.FileName, p.FullName, p.Email }.Any(v => v?.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();

        var items = matching
            .Skip(skip)
            .Take(take)
            .Select(p => new ProfileSummary(p.Id, p.FileName, p.PageCount, p.CreatedAt, p.FolderId, p.FullName, p.Email, p.Phone))
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

    public Task<List<PendingExtraction>> GetPendingExtractionAsync(int take, CancellationToken ct) =>
        Task.FromResult(Profiles
            .Select(p => p.Profile)
            .Where(p => p.FieldsExtractedAt is null)
            .OrderBy(p => p.CreatedAt)
            .Take(take)
            .Select(p => new PendingExtraction(p.Id, p.RawText))
            .ToList());

    public Task SaveExtractedFieldsAsync(Guid id, ExtractedCvFields fields, DateTime extractedAt, CancellationToken ct)
    {
        foreach (var (profile, _) in Profiles.Where(p => p.Profile.Id == id && p.Profile.FieldsExtractedAt is null))
        {
            profile.FullName = fields.FullName;
            profile.Email = fields.Email;
            profile.Phone = fields.Phone;
            profile.FieldsExtractedAt = extractedAt;
        }
        return Task.CompletedTask;
    }

    private IEnumerable<TermEmbedding> JobTerms(Guid ownerId, Guid jobPostingId) =>
        _jobs.Jobs.Where(j => j.Job.Id == jobPostingId && j.Job.OwnerId == ownerId).SelectMany(j => j.Terms);

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
