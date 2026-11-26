namespace Kitaphane.Application.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);

public sealed record VerifyEmailRequest(string Email, string Code);

public sealed record ResendVerificationRequest(string Email);

public sealed record LoginRequest(string Email, string Password, bool RememberMe);

public sealed record ForgotPasswordRequest(string Email);

public sealed record VerifyResetCodeRequest(string Email, string Code);

public sealed record VerifyResetCodeResponse(string ResetToken);

public sealed record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserDto(Guid Id, string Name, string Email, string? ProfileImage);

/// <summary>Body returned to the client after a successful sign-in or token refresh.</summary>
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

/// <summary>Refresh token data the API layer writes to the HttpOnly cookie. Never serialized to the body.</summary>
public sealed record RefreshTokenCookie(string Token, DateTime ExpiresAt, bool IsPersistent);

public sealed record AuthResult(AuthResponse Response, RefreshTokenCookie RefreshToken);
