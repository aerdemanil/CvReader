using CvReader.Application.Abstractions;
using CvReader.Domain.Entities;

namespace CvReader.Application.Cv;

public record CvUploadResult(string FileName, Guid? ProfileId, string? Error);

public class CvUploadService
{
    private readonly ICvParser _cvParser;
    private readonly IProfileRepository _profiles;

    public CvUploadService(ICvParser cvParser, IProfileRepository profiles)
    {
        _cvParser = cvParser;
        _profiles = profiles;
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

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            RawText = parsed.Text,
            PageCount = parsed.PageCount
        };

        try
        {
            await _profiles.AddAsync(profile, ct);
        }
        catch (ProfileSaveException ex)
        {
            return new CvUploadResult(fileName, null, ex.Message);
        }

        return new CvUploadResult(fileName, profile.Id, null);
    }
}