namespace CvReader.Application.Abstractions;

public interface ICvNameExtractor
{
    // cvHead: CV metninin başı. İsim bulunamazsa null döner.
    // Servise ulaşılamazsa CvNameExtractionException fırlatır.
    Task<string?> ExtractNameAsync(string cvHead, CancellationToken ct);
}
