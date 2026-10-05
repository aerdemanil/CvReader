using CvReader.Application.Abstractions;
using CvReader.Application.Embeddings;
using CvReader.Domain.Entities;

namespace CvReader.Application.Cv;

public record CvUploadResult(string FileName, Guid? ProfileId, string? Error);

public class CvUploadService
{
    private readonly ICvParser _cvParser;
    private readonly IProfileRepository _profiles;
    private readonly IEmbeddingService _embeddings;

    public CvUploadService(ICvParser cvParser, IProfileRepository profiles, IEmbeddingService embeddings)
    {
        _cvParser = cvParser;
        _profiles = profiles;
        _embeddings = embeddings;
    }

    public async Task<CvUploadResult> UploadAsync(string fileName, Stream content, CancellationToken ct)
    {
        ParsedCv parsed;
        try
        {
            parsed = _cvParser.Parse(content);
        }
        catch (InvalidCvFileException ex)
        {
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
            return new CvUploadResult(fileName, null, ex.Message);
        }

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            RawText = parsed.Text,
            PageCount = parsed.PageCount
        };

        try
        {
            await _profiles.AddAsync(profile, embedding, ct);
        }
        catch (ProfileSaveException ex)
        {
            return new CvUploadResult(fileName, null, ex.Message);
        }

        return new CvUploadResult(fileName, profile.Id, null);
    }
}