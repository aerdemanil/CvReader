namespace CvReader.Api.Security;

public static class SecurityHeaders
{
    // Ön yüz sadece kendi kaynaklarını yükleyebilir; satır içi script, iframe içine gömülme ve MIME tahmini kapalı.
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            // Swagger UI satır içi script kullandığı için CSP onun dışındaki her yanıta uygulanır.
            if (!context.Request.Path.StartsWithSegments("/swagger"))
                headers.ContentSecurityPolicy = ContentSecurityPolicy;

            await next();
        });
}
