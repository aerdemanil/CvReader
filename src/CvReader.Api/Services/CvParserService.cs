using System.Text;
using UglyToad.PdfPig;

namespace CvReader.Api.Services;

public record ParsedCv(string Text, int PageCount);

public class CvParserService
{
    public ParsedCv Parse(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var text = new StringBuilder();

        foreach (var page in document.GetPages())
        {
           
            var words = page.GetWords().Select(w => w.Text);
            text.AppendLine(string.Join(" ", words));
        }

        return new ParsedCv(text.ToString(), document.NumberOfPages);
    }
}