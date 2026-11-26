namespace Kitaphane.Infrastructure.Identity;

/// <summary>
/// A sign-up waiting for email verification. The user account is only created after the code is confirmed.
/// </summary>
public class PendingRegistration
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTime CodeExpiresAt { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime LastCodeSentAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
