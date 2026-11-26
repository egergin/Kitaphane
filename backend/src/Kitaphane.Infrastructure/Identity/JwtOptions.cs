using Microsoft.Extensions.Configuration;

namespace Kitaphane.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const int MinimumSecretLength = 32;

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "kitaphane-api";

    public string Audience { get; set; } = "kitaphane-web";

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;

    public static void Bind(JwtOptions options, IConfiguration configuration)
    {
        options.Secret = configuration["JWT_SECRET"] ?? string.Empty;
        options.Issuer = configuration["JWT_ISSUER"] is { Length: > 0 } issuer ? issuer : options.Issuer;
        options.Audience = configuration["JWT_AUDIENCE"] is { Length: > 0 } audience ? audience : options.Audience;
        options.AccessTokenMinutes = int.TryParse(configuration["JWT_ACCESS_TOKEN_MINUTES"], out var minutes) ? minutes : options.AccessTokenMinutes;
        options.RefreshTokenDays = int.TryParse(configuration["JWT_REFRESH_TOKEN_DAYS"], out var days) ? days : options.RefreshTokenDays;
    }
}
