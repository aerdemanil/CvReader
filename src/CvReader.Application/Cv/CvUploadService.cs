using System.Security.Cryptography;
using CvReader.Application.Abstractions;
using CvReader.Application.Embeddings;
using CvReader.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CvReader.Application.Cv;

public record CvUploadResult(string FileName, Guid? ProfileId, string? Error);

public class CvUploadService
{
    private readonly ICvParser _cvParser;
    private readonly IProfileRepository _profiles;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<CvUploadService> _logger;

    public CvUploadService(
        ICvParser cvParser,
        IProfileRepository profiles,
        IEmbeddingService embeddings,
        ILogger<CvUploadService> logger)
    {
        _cvParser = cvParser;
        _profiles = profiles;
        _embeddings = embeddings;
        _logger = logger;
    }

    // content baştan okunabilir (seekable) olmalıdır: önce özeti alınır, sonra ayrıştırılır.
    // folderId verilmişse çağıran, klasörün bu kullanıcıya ait olduğunu önceden doğrulamış olmalıdır.
    public async Task<CvUploadResult> UploadAsync(Guid ownerId, Guid? folderId, string fileName, Stream content, CancellationToken ct)
    {
        var contentHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(content, ct));
        content.Position = 0;

        if (await _profiles.ExistsAsync(ownerId, contentHash, ct))
            return new CvUploadResult(fileName, null, "This CV has already been uploaded.");

        ParsedCv parsed;
        try
        {
            parsed = _cvParser.Parse(content);
        }
        catch (InvalidCvFileException ex)
        {
            _logger.LogWarning(ex, "CV {FileName} could not be parsed.", fileName);
            return new CvUploadResult(fileName, null, ex.Message);
        }

        // Kayıttan önce alınır; servis yanıt vermezse vektörsüz profil oluşmaz.
        float[] embedding;
        try
        {
            embedding = await _embeddings.EmbedAsync(parsed.Text, ct);
        }
        catch (EmbeddingException ex)
        {
            _logger.LogError(ex, "Embedding failed for CV {FileName}.", fileName);
            return new CvUploadResult(fileName, null, ex.Message);
        }

        var profile = new Profile
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            FolderId = folderId,
            FileName = fileName,
            ContentHash = contentHash,
            RawText = parsed.Text,
            PageCount = parsed.PageCount
        };

        try
        {
            await _profiles.AddAsync(profile, embedding, ct);
        }
        catch (ProfileSaveException ex)
        {
            _logger.LogError(ex, "CV {FileName} could not be saved.", fileName);
            return new CvUploadResult(fileName, null, ex.Message);
        }

        return new CvUploadResult(fileName, profile.Id, null);
    }
}
