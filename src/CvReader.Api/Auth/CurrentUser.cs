using System.Security.Claims;

namespace CvReader.Api.Auth;

public static class CurrentUser
{
    // Yalnızca kimliği doğrulanmış isteklerde çağrılır; token'ı bu uygulama ürettiği için claim her zaman vardır.
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimNames.UserId)!);

    public static bool TryGetSession(this ClaimsPrincipal user, out Guid userId, out int tokenVersion)
    {
        tokenVersion = 0;
        return Guid.TryParse(user.FindFirstValue(ClaimNames.UserId), out userId)
               && int.TryParse(user.FindFirstValue(ClaimNames.TokenVersion), out tokenVersion);
    }
}
