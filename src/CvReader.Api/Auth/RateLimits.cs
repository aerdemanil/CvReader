using System.Security.Claims;
using System.Threading.RateLimiting;

namespace CvReader.Api.Auth;

public static class RateLimits
{
    public const string Auth = "auth";
    public const string Upload = "upload";
    public const string Write = "write";

    public static IServiceCollection AddRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Şifre deneme (brute force) saldırılarına karşı: IP başına dakikada 10 giriş/kayıt isteği.
            options.AddPolicy(Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientIp(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

            // Toplu yükleme CPU'yu ve embedding servisini uzun süre meşgul eder: kullanıcı başına aynı anda tek yükleme.
            options.AddPolicy(Upload, context => RateLimitPartition.GetConcurrencyLimiter(
                UserOrIp(context),
                _ => new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 }));

            // Her ilan oluşturma bir embedding isteği demektir: kullanıcı başına dakikada 30.
            options.AddPolicy(Write, context => RateLimitPartition.GetFixedWindowLimiter(
                UserOrIp(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
        });

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string UserOrIp(HttpContext context) =>
        context.User.FindFirstValue(ClaimNames.UserId) ?? ClientIp(context);
}
