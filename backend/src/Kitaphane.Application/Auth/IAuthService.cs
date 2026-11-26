namespace Kitaphane.Application.Auth;

public interface IAuthService
{
    /// <summary>Stores a pending registration and emails a verification code. No user is created yet.</summary>
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>Checks the code, creates the user and signs them in.</summary>
    Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);

    Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken);

    /// <summary>Emails a password reset code when the account exists. Always completes silently.</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Checks the reset code and returns a short-lived reset token for <see cref="ResetPasswordAsync"/>.</summary>
    Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request, CancellationToken cancellationToken);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? currentRefreshToken, CancellationToken cancellationToken);

    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
