using CvReader.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CvReader.Application.Cv;

public class CvFieldExtractionService
{
    // Kolon varchar(150).
    public const int MaxNameLength = 150;

    // İsim CV'nin başında yer alır; metnin tamamı modele gönderilmez.
    private const int NameSearchLength = 1500;

    private readonly IProfileRepository _profiles;
    private readonly ICvNameExtractor _names;
    private readonly ILogger<CvFieldExtractionService> _logger;

    public CvFieldExtractionService(
        IProfileRepository profiles,
        ICvNameExtractor names,
        ILogger<CvFieldExtractionService> logger)
    {
        _profiles = profiles;
        _names = names;
        _logger = logger;
    }

    // Çıkarım bekleyen en eski CV'leri işler ve işlenen CV sayısını döner.
    // İsim servisine ulaşılamazsa kalan CV'ler beklemede kalır ve sonraki çağrıda yeniden denenir.
    public async Task<int> ProcessPendingAsync(int batchSize, CancellationToken ct)
    {
        var pending = await _profiles.GetPendingExtractionAsync(batchSize, ct);
        var processed = 0;

        foreach (var cv in pending)
        {
            var head = cv.RawText[..Math.Min(cv.RawText.Length, NameSearchLength)];

            string? name;
            try
            {
                name = await _names.ExtractNameAsync(head, ct);
            }
            catch (CvNameExtractionException ex)
            {
                _logger.LogWarning(ex, "Name extraction is unavailable; CV {ProfileId} stays pending.", cv.Id);
                break;
            }

            var fields = new ExtractedCvFields(
                ValidName(name, head),
                ContactExtractor.ExtractEmail(cv.RawText),
                ContactExtractor.ExtractPhone(cv.RawText));

            await _profiles.SaveExtractedFieldsAsync(cv.Id, fields, DateTime.UtcNow, ct);
            processed++;
        }

        return processed;
    }

    // Model çıktısı güvenilmezdir: isim yalnızca CV metninde gerçekten geçiyorsa kabul edilir.
    private static string? ValidName(string? name, string head)
    {
        if (name is null) return null;

        var candidate = CollapseWhitespace(name);

        return candidate.Length is >= 2 and <= MaxNameLength
            && candidate.Any(char.IsLetter)
            && !candidate.Any(char.IsDigit)
            && !candidate.Contains('@')
            && Fold(CollapseWhitespace(head)).Contains(Fold(candidate), StringComparison.Ordinal)
                ? candidate
                : null;
    }

    private static string CollapseWhitespace(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // Büyük/küçük harf ve Türkçe noktalı/noktasız i farkı karşılaştırmayı bozmasın: "YILMAZ" ile "Yılmaz" eşleşir.
    private static string Fold(string text) =>
        text.Replace('İ', 'I').Replace('ı', 'i').ToUpperInvariant();
}
