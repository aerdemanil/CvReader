using System.Text;
using CvReader.Application.Cv;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;

namespace CvReader.Infrastructure.Parsing;

public class PdfPigCvParser : ICvParser
{
    public ParsedCv Parse(Stream stream)
    {
        try
        {
            using var document = PdfDocument.Open(stream);
            var text = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().Select(w => w.Text);
                text.AppendLine(string.Join(" ", words));
            }

            return new ParsedCv(text.ToString(), document.NumberOfPages);
        }
        catch (PdfDocumentFormatException ex)
        {
            throw new InvalidCvFileException("The file could not be read as a valid PDF.", ex);
        }
    }
}
