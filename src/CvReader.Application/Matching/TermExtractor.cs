using System.Text.RegularExpressions;

namespace CvReader.Application.Matching;

public static partial class TermExtractor
{
    // Kolon varchar(100); daha uzun parçalar (ör. URL) terim sayılmaz.
    public const int MaxTermLength = 100;

    // Çok uzun bir PDF embedding servisini ve veritabanını dakikalarca meşgul edemesin.
    public const int MaxTerms = 3000;

    // "sql server" ya da "makine öğrenmesi" gibi ifadeler de terimdir.
    public const int MaxPhraseWords = 3;

    // Aynı şeyin farklı yazımları model için uzak kalır ("c#"-"csharp" 0.58); hepsi tek yazıma indirilir.
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["csharp"] = "c#",
        ["dotnet"] = ".net",
        ["js"] = "javascript",
        ["mssql"] = "sql server",
        ["nodejs"] = "node.js",
        ["postgres"] = "postgresql",
        ["k8s"] = "kubernetes"
    };

    // "c#", "c++", ".net", "node.js" ve "back-end" tek kelime olarak kalır.
    [GeneratedRegex(@"\.?[\p{L}\p{N}][\p{L}\p{N}#+.\-]*")]
    private static partial Regex WordPattern();

    // Metindeki farklı kelimeler ve en çok MaxPhraseWords kelimelik ifadeler, ilk geçtikleri sırayla ve küçük harfle.
    public static List<string> Extract(string text) =>
        Runs(text)
            .SelectMany(Phrases)
            .Where(t => t.Length <= MaxTermLength)
            .Distinct()
            .Take(MaxTerms)
            .ToList();

    // Anahtar kelime CV ile aynı kurala göre okunur: "SQL Server" tek terimdir (sql server), "CI/CD" iki terim (ci, cd).
    // "C" gibi kelime sayılmayanlar olduğu gibi kullanılır.
    public static List<string> ExtractFromKeyword(string keyword)
    {
        var terms = Runs(keyword)
            .Select(run => string.Join(' ', run))
            .Where(t => t.Length <= MaxTermLength)
            .Distinct()
            .ToList();

        return terms.Count > 0 ? terms : [keyword.ToLowerInvariant()];
    }

    // Araya noktalama, sayı ya da tek harf girmeden art arda gelen kelime grupları.
    private static IEnumerable<List<string>> Runs(string text)
    {
        var lowered = text.ToLowerInvariant();
        var run = new List<string>();
        var previousEnd = 0;

        foreach (Match match in WordPattern().Matches(lowered))
        {
            var word = match.Value.TrimEnd('.', '-');
            var isWord = word.Length is >= 2 and <= MaxTermLength && word.Any(char.IsLetter);
            var followsPrevious = string.IsNullOrWhiteSpace(lowered[previousEnd..match.Index]);

            if (run.Count > 0 && !(isWord && followsPrevious))
            {
                yield return run;
                run = [];
            }

            if (isWord)
                run.Add(Aliases.GetValueOrDefault(word, word));

            // Sondan kırpılan nokta ve tire aralığa dahil olur; cümle sonu ifadeyi böler.
            previousEnd = match.Index + word.Length;
        }

        if (run.Count > 0)
            yield return run;
    }

    // Grubun her kelimesi ve o kelimede biten ifadeler: [sql, server] -> sql, server, sql server.
    private static IEnumerable<string> Phrases(List<string> run)
    {
        for (var end = 0; end < run.Count; end++)
            for (var words = 1; words <= Math.Min(MaxPhraseWords, end + 1); words++)
                yield return string.Join(' ', run.GetRange(end - words + 1, words));
    }
}
