namespace CvReader.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CvReader";
    public string Audience { get; set; } = "CvReader";
    public int ExpiryMinutes { get; set; } = 60;
}
