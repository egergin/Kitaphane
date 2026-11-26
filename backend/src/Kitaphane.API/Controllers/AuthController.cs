using Kitaphane.API.Auth;
using Kitaphane.Application.Auth;
using Kitaphane.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kitaphane.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy)]
public sealed class AuthController(IAuthService authService, ICurrentUser currentUser, IWebHostEnvironment environment)
    : ControllerBase
{
    /// <summary>Starts sign-up and emails a verification code. The account is created by verify-email.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await authService.RegisterAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>Confirms the emailed code, creates the account and signs the user in.</summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken) =>
        SignedIn(await authService.VerifyEmailAsync(request, cancellationToken));

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        await authService.ResendVerificationAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        SignedIn(await authService.LoginAsync(request, cancellationToken));

    /// <summary>Issues a new access token using the refresh token cookie (the cookie is rotated).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            return SignedIn(await authService.RefreshAsync(RefreshTokenCookies.Read(Request), cancellationToken));
        }
        catch
        {
            RefreshTokenCookies.Delete(Response, environment);
            throw;
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(RefreshTokenCookies.Read(Request), cancellationToken);
        RefreshTokenCookies.Delete(Response, environment);
        return NoContent();
    }

    /// <summary>Emails a password reset code if the account exists. Always returns 202.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>Confirms the password reset code and returns a short-lived reset token.</summary>
    [HttpPost("verify-reset-code")]
    [AllowAnonymous]
    public async Task<ActionResult<VerifyResetCodeResponse>> VerifyResetCode(
        VerifyResetCodeRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.VerifyResetCodeAsync(request, cancellationToken));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(
            currentUser.RequireUserId(), request, RefreshTokenCookies.Read(Request), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await authService.GetCurrentUserAsync(currentUser.RequireUserId(), cancellationToken));

    private ActionResult<AuthResponse> SignedIn(AuthResult result)
    {
        RefreshTokenCookies.Write(Response, result.RefreshToken, environment);
        return Ok(result.Response);
    }
}
