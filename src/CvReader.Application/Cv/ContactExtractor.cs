using System.Text.RegularExpressions;

namespace CvReader.Application.Cv;

public static partial class ContactExtractor
{
    // Kolon varchar(256); daha uzun bir eşleşme kesilmez, bulunamadı sayılır.
    public const int MaxEmailLength = 256;

    // Kolon varchar(32); normalize edilmiş numara en çok 16 karakterdir.
    public const int MaxPhoneLength = 32;

    // CV metni güvenilmez girdidir; kötü niyetli bir metin eşleştirmeyi dakikalarca sürdüremesin.
    private const int MatchTimeoutMilliseconds = 1000;

    [GeneratedRegex(@"[\p{L}\p{N}._%+\-]+@[\p{L}\p{N}\-]+(?:\.[\p{L}\p{N}\-]+)+", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex EmailPattern();

    // Rakamlar arasında en çok iki ayraç: "+90 (532) 111 22 33" eşleşir, "2019 - 2023" eşleşmez.
    // Nokta ayraç sayılmaz; yoksa "01.01.2019" gibi tarihler telefon sanılır.
    [GeneratedRegex(@"(?<!\d)\+?\(?\d(?:[ \t()\-]{0,2}\d){8,14}(?!\d)", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex PhonePattern();

    // Metindeki ilk e-posta adresi, küçük harfle.
    public static string? ExtractEmail(string text)
    {
        try
        {
            var match = EmailPattern().Match(text);
            return match.Success && match.Length <= MaxEmailLength ? match.Value.ToLowerInvariant() : null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    // Metindeki ilk telefon numarası; yalnızca rakamlar ve varsa baştaki "+".
    public static string? ExtractPhone(string text)
    {
        try
        {
            return PhonePattern().Matches(text)
                .Select(m => Normalize(m.Value))
                .FirstOrDefault(IsPhone);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static string Normalize(string match) =>
        (match.StartsWith('+') ? "+" : string.Empty) + string.Concat(match.Where(char.IsAsciiDigit));

    // Yıl aralıkları ve kimlik numaraları elensin diye numara "+" ya da "0" ile başlamalı
    // veya başında sıfır olmadan yazılmış bir cep numarası (5xx xxx xx xx) olmalıdır.
    private static bool IsPhone(string phone)
    {
        var digits = phone.TrimStart('+');

        return digits.Length is >= 10 and <= 15
            && (phone[0] is '+' or '0' || (digits.Length == 10 && digits[0] == '5'));
    }
}
