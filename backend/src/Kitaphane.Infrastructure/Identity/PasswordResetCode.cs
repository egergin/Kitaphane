namespace Kitaphane.Infrastructure.Identity;

/// <summary>
/// Password reset state of a user: first the emailed code, then (after the code is verified) a short-lived reset token.
/// </summary>
public class PasswordResetCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Null once the code has been verified.</summary>
    public string? CodeHash { get; set; }

    public DateTime CodeExpiresAt { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime LastCodeSentAt { get; set; }

    public string? ResetTokenHash { get; set; }

    public DateTime? ResetTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
