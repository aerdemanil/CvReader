using System.Text;
using CvReader.Application.Cv;
using UglyToad.PdfPig;

namespace CvReader.Infrastructure.Parsing;

public class PdfPigCvParser : ICvParser
{
    private const int MaxPages = 50;

    // PDF belirtimi imzanın dosyanın ilk 1024 baytı içinde olmasına izin verir.
    private const int SignatureSearchLength = 1024;
    private static readonly byte[] Signature = "%PDF-"u8.ToArray();

    public ParsedCv Parse(Stream stream)
    {
        if (!HasPdfSignature(stream))
            throw new InvalidCvFileException("The file is not a PDF.");

        try
        {
            using var document = PdfDocument.Open(stream);

            if (document.NumberOfPages > MaxPages)
                throw new InvalidCvFileException($"The PDF has more than {MaxPages} pages.");

            var text = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().Select(w => w.Text);
                text.AppendLine(string.Join(" ", words));
            }

            // PostgreSQL text kolonları \0 karakterini kabul etmez; bazı PDF'ler bunu üretir.
            var cleanText = text.ToString().Replace("\0", string.Empty);

            if (string.IsNullOrWhiteSpace(cleanText))
                throw new InvalidCvFileException("The PDF contains no readable text (it may be a scanned image).");

            return new ParsedCv(cleanText, document.NumberOfPages);
        }
        // PdfPig şifreli ya da bozuk dosyalarda ortak bir tabanı olmayan çok sayıda istisna türü fırlatır;
        // kullanıcının yüklediği hiçbir dosya isteği düşürmemeli.
        catch (Exception ex) when (ex is not InvalidCvFileException)
        {
            throw new InvalidCvFileException("The file could not be read as a valid PDF.", ex);
        }
    }

    private static bool HasPdfSignature(Stream stream)
    {
        var buffer = new byte[SignatureSearchLength];
        var read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        stream.Position = 0;

        return buffer.AsSpan(0, read).IndexOf(Signature) >= 0;
    }
}
