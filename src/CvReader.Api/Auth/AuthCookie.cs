namespace CvReader.Api.Auth;

// JWT tarayıcıda JavaScript'in okuyamayacağı bir cookie'de taşınır:
// HttpOnly → XSS ile çalınamaz, SameSite=Strict → başka sitelerden gelen isteklere eklenmez (CSRF).
public static class AuthCookie
{
    public const string Name = "cvreader_session";

    public static void Append(HttpResponse response, string token, DateTime expiresAt) =>
        response.Cookies.Append(Name, token, CreateOptions(expiresAt));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, CreateOptions(expires: null));

    private static CookieOptions CreateOptions(DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Expires = expires
    };
}
