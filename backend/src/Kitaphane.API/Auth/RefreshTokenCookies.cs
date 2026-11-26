using Kitaphane.Application.Auth;

namespace Kitaphane.API.Auth;

/// <summary>
/// Writes and reads the HttpOnly refresh token cookie. It is only sent to /api/auth endpoints.
/// </summary>
public static class RefreshTokenCookies
{
    public const string Name = "kitaphane_rt";
    private const string Path = "/api/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Write(HttpResponse response, RefreshTokenCookie refreshToken, IWebHostEnvironment environment)
    {
        response.Cookies.Append(Name, refreshToken.Token, CreateOptions(response.HttpContext, environment, refreshToken));
    }

    public static void Delete(HttpResponse response, IWebHostEnvironment environment)
    {
        response.Cookies.Delete(Name, CreateOptions(response.HttpContext, environment, refreshToken: null));
    }

    private static CookieOptions CreateOptions(HttpContext context, IWebHostEnvironment environment, RefreshTokenCookie? refreshToken) => new()
    {
        HttpOnly = true,
        // Always Secure outside Development; local development runs over plain HTTP.
        Secure = !environment.IsDevelopment() || context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        IsEssential = true,
        // Without "keep me signed in" the cookie lives only for the browser session.
        Expires = refreshToken is { IsPersistent: true } ? refreshToken.ExpiresAt : null,
    };
}
