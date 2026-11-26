namespace Kitaphane.Application.Auth;

/// <summary>Shared authentication limits (documented in docs/authentication/authentication.md).</summary>
public static class AuthRules
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 256;

    public const int CodeLength = 6;
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan CodeResendCooldown = TimeSpan.FromSeconds(60);
    public const int MaxCodeAttempts = 5;

    public static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(15);
}
