using FluentValidation;
using FluentValidation.Results;
using Kitaphane.Application.Auth;
using Kitaphane.Application.Common;
using Kitaphane.Application.Common.Errors;
using Kitaphane.Infrastructure.Email;
using Kitaphane.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kitaphane.Infrastructure.Identity;

/// <summary>
/// Sign-up with email verification, JWT sign-in with rotating refresh tokens and code-based password reset.
/// Flows are described in docs/authentication/authentication.md.
/// </summary>
public sealed class AuthService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IPasswordHasher<ApplicationUser> passwordHasher,
    TokenService tokenService,
    SecretHasher hasher,
    IEmailSender emailSender,
    TimeProvider timeProvider) : IAuthService
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    // ---------------------------------------------------------------- Registration

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var normalizedEmail = Normalize(email);
        var now = Now;

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException(ErrorCodes.EmailAlreadyExists, "An account with this email already exists.");
        }

        await db.PendingRegistrations
            .Where(pending => pending.CodeExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        var registration = await db.PendingRegistrations
            .SingleOrDefaultAsync(pending => pending.NormalizedEmail == normalizedEmail, cancellationToken);

        if (registration is null)
        {
            registration = new PendingRegistration
            {
                Id = Guid.CreateVersion7(),
                NormalizedEmail = normalizedEmail,
                CreatedAt = now,
            };
            db.PendingRegistrations.Add(registration);
        }

        registration.Email = email;
        registration.Name = request.Name.Trim();
        registration.PasswordHash = passwordHasher.HashPassword(new ApplicationUser(), request.Password);

        // Repeated sign-up within the cooldown updates the details but keeps the code already sent.
        var code = CanSendCode(registration.LastCodeSentAt, now) ? IssueCode(registration, now) : null;

        await db.SaveChangesAsync(cancellationToken);

        if (code is not null)
        {
            await emailSender.SendAsync(EmailTemplates.VerificationCode(email, registration.Name, code), cancellationToken);
        }
    }

    public async Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Normalize(request.Email);
        var now = Now;

        var registration = await db.PendingRegistrations
            .SingleOrDefaultAsync(pending => pending.NormalizedEmail == normalizedEmail, cancellationToken)
            ?? throw new BadRequestException(ErrorCodes.VerificationCodeInvalid, "Verification code is invalid.");

        await EnsureCodeMatchesAsync(
            request.Code,
            registration.CodeHash,
            registration.CodeExpiresAt,
            () => registration.FailedAttempts,
            attempts => registration.FailedAttempts = attempts,
            now,
            cancellationToken);

        if (await userManager.FindByEmailAsync(registration.Email) is not null)
        {
            db.PendingRegistrations.Remove(registration);
            await db.SaveChangesAsync(cancellationToken);
            throw new ConflictException(ErrorCodes.EmailAlreadyExists, "An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = registration.Email,
            Email = registration.Email,
            EmailConfirmed = true,
            Name = registration.Name,
            PasswordHash = registration.PasswordHash,
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"User creation failed: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }

        db.PendingRegistrations.Remove(registration);
        await db.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, isPersistent: true, cancellationToken);
    }

    public async Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Normalize(request.Email);
        var now = Now;

        var registration = await db.PendingRegistrations
            .SingleOrDefaultAsync(pending => pending.NormalizedEmail == normalizedEmail, cancellationToken);

        // No pending registration or still in cooldown: complete silently (no account enumeration).
        if (registration is null || !CanSendCode(registration.LastCodeSentAt, now))
        {
            return;
        }

        var code = IssueCode(registration, now);
        await db.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            EmailTemplates.VerificationCode(registration.Email, registration.Name, code), cancellationToken);
    }

    // ---------------------------------------------------------------- Sign-in and tokens

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException(ErrorCodes.InvalidCredentials, "Email or password is incorrect.");
        }

        return await IssueTokensAsync(user, request.RememberMe, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        var now = Now;
        var token = await FindRefreshTokenAsync(refreshToken, cancellationToken) ?? throw InvalidRefreshToken();

        if (token.RevokedAt is not null)
        {
            if (token.ReplacedByTokenId is not null)
            {
                // A rotated token was presented again: assume it was stolen and end every session of the user.
                await RevokeRefreshTokensAsync(token.UserId, exceptTokenId: null, now, cancellationToken);
            }

            throw InvalidRefreshToken();
        }

        if (token.ExpiresAt <= now)
        {
            throw InvalidRefreshToken();
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString()) ?? throw InvalidRefreshToken();

        var (rawToken, newToken) = tokenService.CreateRefreshToken(user.Id, token.IsPersistent, now);
        token.RevokedAt = now;
        token.ReplacedByTokenId = newToken.Id;
        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync(cancellationToken);

        return CreateAuthResult(user, rawToken, newToken, now);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        var token = await FindRefreshTokenAsync(refreshToken, cancellationToken);

        if (token is not null && token.IsActive(Now))
        {
            token.RevokedAt = Now;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    // ---------------------------------------------------------------- Password reset (code based)

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var now = Now;
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        // Unknown email: complete silently (no account enumeration).
        if (user is null)
        {
            return;
        }

        var reset = await db.PasswordResetCodes.SingleOrDefaultAsync(code => code.UserId == user.Id, cancellationToken);

        if (reset is null)
        {
            reset = new PasswordResetCode { Id = Guid.CreateVersion7(), UserId = user.Id, CreatedAt = now };
            db.PasswordResetCodes.Add(reset);
        }
        else if (!CanSendCode(reset.LastCodeSentAt, now))
        {
            return;
        }

        var code = SecretHasher.NewNumericCode();
        reset.CodeHash = hasher.Hash(code);
        reset.CodeExpiresAt = now.Add(AuthRules.CodeLifetime);
        reset.FailedAttempts = 0;
        reset.LastCodeSentAt = now;
        reset.ResetTokenHash = null;
        reset.ResetTokenExpiresAt = null;

        await db.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(EmailTemplates.PasswordResetCode(user.Email!, user.Name, code), cancellationToken);
    }

    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(
        VerifyResetCodeRequest request, CancellationToken cancellationToken)
    {
        var now = Now;
        var reset = await FindPasswordResetAsync(request.Email, cancellationToken);

        if (reset?.CodeHash is null)
        {
            throw new BadRequestException(ErrorCodes.VerificationCodeInvalid, "Verification code is invalid.");
        }

        await EnsureCodeMatchesAsync(
            request.Code,
            reset.CodeHash,
            reset.CodeExpiresAt,
            () => reset.FailedAttempts,
            attempts => reset.FailedAttempts = attempts,
            now,
            cancellationToken);

        var resetToken = SecretHasher.NewToken();
        reset.CodeHash = null; // A code can only be used once.
        reset.ResetTokenHash = hasher.Hash(resetToken);
        reset.ResetTokenExpiresAt = now.Add(AuthRules.ResetTokenLifetime);
        await db.SaveChangesAsync(cancellationToken);

        return new VerifyResetCodeResponse(resetToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var now = Now;
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        var reset = user is null
            ? null
            : await db.PasswordResetCodes.SingleOrDefaultAsync(code => code.UserId == user.Id, cancellationToken);

        if (user is null
            || reset?.ResetTokenExpiresAt is null
            || reset.ResetTokenExpiresAt <= now
            || !hasher.Verify(request.ResetToken, reset.ResetTokenHash))
        {
            throw new BadRequestException(ErrorCodes.InvalidResetToken, "Password reset session is invalid or expired.");
        }

        EnsureDiffersFromCurrentPassword(user, request.NewPassword);

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, identityToken, request.NewPassword);
        ThrowIfFailed(result, nameof(ResetPasswordRequest.NewPassword));

        db.PasswordResetCodes.Remove(reset);
        await RevokeRefreshTokensAsync(user.Id, exceptTokenId: null, now, cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId, ChangePasswordRequest request, string? currentRefreshToken, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw UnauthorizedUser();

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            throw new BadRequestException(ErrorCodes.InvalidCurrentPassword, "Current password is incorrect.");
        }

        EnsureDiffersFromCurrentPassword(user, request.NewPassword);

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        ThrowIfFailed(result, nameof(ChangePasswordRequest.NewPassword));

        // Sign out other sessions; keep the one that made the change.
        var currentToken = await FindRefreshTokenAsync(currentRefreshToken, cancellationToken);
        var keepTokenId = currentToken?.UserId == user.Id ? currentToken.Id : (Guid?)null;
        await RevokeRefreshTokensAsync(user.Id, keepTokenId, Now, cancellationToken);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw UnauthorizedUser();
        return ToDto(user);
    }

    // ---------------------------------------------------------------- Helpers

    private async Task<AuthResult> IssueTokensAsync(ApplicationUser user, bool isPersistent, CancellationToken cancellationToken)
    {
        var now = Now;

        // Housekeeping: expired tokens are no longer needed for reuse detection.
        await db.RefreshTokens
            .Where(token => token.UserId == user.Id && token.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        var (rawToken, refreshToken) = tokenService.CreateRefreshToken(user.Id, isPersistent, now);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        return CreateAuthResult(user, rawToken, refreshToken, now);
    }

    private AuthResult CreateAuthResult(ApplicationUser user, string rawRefreshToken, RefreshToken refreshToken, DateTime now)
    {
        var accessToken = tokenService.CreateAccessToken(user, now);

        return new AuthResult(
            new AuthResponse(accessToken.Token, accessToken.ExpiresAt, ToDto(user)),
            new RefreshTokenCookie(rawRefreshToken, refreshToken.ExpiresAt, refreshToken.IsPersistent));
    }

    private async Task<RefreshToken?> FindRefreshTokenAsync(string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = hasher.Hash(rawToken);
        return await db.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    private async Task RevokeRefreshTokensAsync(Guid userId, Guid? exceptTokenId, DateTime now, CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);

        await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.Id != exceptTokenId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    private async Task<PasswordResetCode?> FindPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        return user is null
            ? null
            : await db.PasswordResetCodes.SingleOrDefaultAsync(code => code.UserId == user.Id, cancellationToken);
    }

    private string IssueCode(PendingRegistration registration, DateTime now)
    {
        var code = SecretHasher.NewNumericCode();
        registration.CodeHash = hasher.Hash(code);
        registration.CodeExpiresAt = now.Add(AuthRules.CodeLifetime);
        registration.FailedAttempts = 0;
        registration.LastCodeSentAt = now;
        return code;
    }

    private static bool CanSendCode(DateTime lastSentAt, DateTime now) =>
        now - lastSentAt >= AuthRules.CodeResendCooldown;

    /// <summary>Checks attempts, expiry and the code itself; failed attempts are persisted before throwing.</summary>
    private async Task EnsureCodeMatchesAsync(
        string code,
        string? codeHash,
        DateTime expiresAt,
        Func<int> getAttempts,
        Action<int> setAttempts,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (getAttempts() >= AuthRules.MaxCodeAttempts)
        {
            throw new BadRequestException(ErrorCodes.VerificationAttemptsExceeded, "Too many incorrect attempts. Request a new code.");
        }

        if (expiresAt <= now)
        {
            throw new BadRequestException(ErrorCodes.VerificationCodeExpired, "Verification code has expired.");
        }

        if (!hasher.Verify(code, codeHash))
        {
            var attempts = getAttempts() + 1;
            setAttempts(attempts);
            await db.SaveChangesAsync(cancellationToken);

            throw attempts >= AuthRules.MaxCodeAttempts
                ? new BadRequestException(ErrorCodes.VerificationAttemptsExceeded, "Too many incorrect attempts. Request a new code.")
                : new BadRequestException(ErrorCodes.VerificationCodeInvalid, "Verification code is invalid.");
        }
    }

    private void EnsureDiffersFromCurrentPassword(ApplicationUser user, string newPassword)
    {
        if (user.PasswordHash is not null
            && passwordHasher.VerifyHashedPassword(user, user.PasswordHash, newPassword) != PasswordVerificationResult.Failed)
        {
            throw new BadRequestException(ErrorCodes.PasswordSameAsCurrent, "New password must be different from the current password.");
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string propertyName)
    {
        if (!result.Succeeded)
        {
            throw new ValidationException(
                result.Errors.Select(error => new ValidationFailure(propertyName, error.Description)));
        }
    }

    private string Normalize(string email) => userManager.NormalizeEmail(email.Trim());

    private static UserDto ToDto(ApplicationUser user) => new(user.Id, user.Name, user.Email!, user.ProfileImage);

    private static UnauthorizedException InvalidRefreshToken() =>
        new(ErrorCodes.InvalidRefreshToken, "Session has expired. Please sign in again.");

    private static UnauthorizedException UnauthorizedUser() =>
        new(ErrorCodes.Unauthorized, "Authentication is required.");
}
