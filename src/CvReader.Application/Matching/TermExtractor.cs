using System.Text.RegularExpressions;

namespace CvReader.Application.Matching;

public static partial class TermExtractor
{
    // Kolon varchar(100); daha uzun parçalar (ör. URL) terim sayılmaz.
    public const int MaxTermLength = 100;

    // Çok uzun bir PDF embedding servisini ve veritabanını dakikalarca meşgul edemesin.
    public const int MaxTerms = 1000;

    // "c#", "c++", ".net", "node.js" ve "back-end" tek terim olarak kalır.
    [GeneratedRegex(@"\.?[\p{L}\p{N}][\p{L}\p{N}#+.\-]*")]
    private static partial Regex TermPattern();

    // Metindeki farklı terimler, ilk geçtikleri sırayla ve küçük harfle.
    public static List<string> Extract(string text) =>
        TermPattern().Matches(text.ToLowerInvariant())
            .Select(m => m.Value.TrimEnd('.', '-'))
            .Where(t => t.Length is >= 2 and <= MaxTermLength && t.Any(char.IsLetter))
            .Distinct()
            .Take(MaxTerms)
            .ToList();

    // Anahtar kelime CV ile aynı kurala göre bölünür ("sql server" -> sql, server);
    // "C" gibi bölünemeyenler olduğu gibi kullanılır.
    public static List<string> ExtractFromKeyword(string keyword)
    {
        var terms = Extract(keyword);
        return terms.Count > 0 ? terms : [keyword.ToLowerInvariant()];
    }
}
