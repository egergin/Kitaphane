namespace Kitaphane.Application.Common;

/// <summary>Shared URL checks for image fields (book cover, profile image).</summary>
public static class UrlRules
{
    public const int ImageUrlMaxLength = 2048;

    public static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
