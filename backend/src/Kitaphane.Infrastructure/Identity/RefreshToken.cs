namespace Kitaphane.Infrastructure.Identity;

/// <summary>
/// A refresh token issued to a user. Only the hash of the token is stored; the raw value lives in the HttpOnly cookie.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    /// <summary>Set when the token was rotated. Presenting a rotated token again signals token theft.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>"Keep me signed in": persistent cookie when true, browser-session cookie otherwise.</summary>
    public bool IsPersistent { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
