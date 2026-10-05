using CvReader.Application.Cv;
using CvReader.Infrastructure.Parsing;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace CvReader.Tests;

public class PdfPigCvParserTests
{
    private readonly PdfPigCvParser _parser = new();

    private static MemoryStream Pdf(int pages, string? text)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (var i = 0; i < pages; i++)
        {
            var page = builder.AddPage(PageSize.A4);
            if (text is not null) page.AddText(text, 12, new PdfPoint(25, 700), font);
        }

        return new MemoryStream(builder.Build());
    }

    [Fact]
    public void Pdf_with_text_is_parsed()
    {
        var parsed = _parser.Parse(Pdf(pages: 2, "Senior backend developer"));

        Assert.Equal(2, parsed.PageCount);
        Assert.Contains("backend", parsed.Text);
    }

    [Fact]
    public void File_without_pdf_signature_is_rejected()
    {
        var stream = new MemoryStream("PK\u0003\u0004 this is a zip file"u8.ToArray());

        Assert.Throws<InvalidCvFileException>(() => _parser.Parse(stream));
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        Assert.Throws<InvalidCvFileException>(() => _parser.Parse(new MemoryStream()));
    }

    [Fact]
    public void Corrupt_pdf_is_rejected_instead_of_crashing()
    {
        var stream = new MemoryStream("%PDF-1.7\nthis is not a real pdf body"u8.ToArray());

        Assert.Throws<InvalidCvFileException>(() => _parser.Parse(stream));
    }

    [Fact]
    public void Pdf_without_text_is_rejected()
    {
        Assert.Throws<InvalidCvFileException>(() => _parser.Parse(Pdf(pages: 1, text: null)));
    }

    [Fact]
    public void Pdf_with_too_many_pages_is_rejected()
    {
        Assert.Throws<InvalidCvFileException>(() => _parser.Parse(Pdf(pages: 51, "text")));
    }
}
