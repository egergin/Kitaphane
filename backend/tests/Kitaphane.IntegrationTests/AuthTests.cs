using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(KitaphaneApiFactory factory)
{
    private readonly AuthApi _api = new(factory.CreateApiClient(), factory.Emails);
    private FakeEmailSender Emails => factory.Emails;

    // ------------------------------------------------------------ Registration

    [Fact]
    public async Task Register_does_not_create_account_until_email_is_verified()
    {
        var email = AuthApi.NewEmail();

        var register = await _api.PostAsync("register", new { name = "Ayşe", email, password = "Password123" });
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        // No account yet: signing in fails.
        var login = await _api.PostAsync("login", new { email, password = "Password123", rememberMe = false });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await AuthApi.ReadErrorCodeAsync(login));

        var verify = await _api.PostAsync("verify-email", new { email, code = Emails.LatestCodeFor(email) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        var session = await Session.FromResponseAsync(verify, email, "Password123");
        var me = await _api.GetMeAsync(session.AccessToken);
        var user = await me.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal("Ayşe", user.GetProperty("name").GetString());
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Contains("httponly", session.SetCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", session.SetCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", session.SetCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_conflict()
    {
        var session = await _api.CreateAccountAsync();

        var response = await _api.PostAsync("register", new { name = "X", email = session.Email.ToUpperInvariant(), password = "Password123" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("EMAIL_ALREADY_EXISTS", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("1234567890")]
    public async Task Register_rejects_weak_passwords(string password)
    {
        var response = await _api.PostAsync("register", new { name = "X", email = AuthApi.NewEmail(), password });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Verification_code_allows_five_attempts()
    {
        var email = AuthApi.NewEmail();
        await _api.PostAsync("register", new { name = "X", email, password = "Password123" });
        var correctCode = Emails.LatestCodeFor(email);
        var wrongCode = correctCode == "000000" ? "111111" : "000000";

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var wrong = await _api.PostAsync("verify-email", new { email, code = wrongCode });
            Assert.Equal("VERIFICATION_CODE_INVALID", await AuthApi.ReadErrorCodeAsync(wrong));
        }

        var fifth = await _api.PostAsync("verify-email", new { email, code = wrongCode });
        Assert.Equal("VERIFICATION_ATTEMPTS_EXCEEDED", await AuthApi.ReadErrorCodeAsync(fifth));

        // Even the correct code is rejected now; a new code must be requested.
        var correct = await _api.PostAsync("verify-email", new { email, code = correctCode });
        Assert.Equal(HttpStatusCode.BadRequest, correct.StatusCode);
        Assert.Equal("VERIFICATION_ATTEMPTS_EXCEEDED", await AuthApi.ReadErrorCodeAsync(correct));
    }

    [Fact]
    public async Task Resend_within_cooldown_does_not_send_another_code()
    {
        var email = AuthApi.NewEmail();
        await _api.PostAsync("register", new { name = "X", email, password = "Password123" });

        var resend = await _api.PostAsync("resend-verification", new { email });

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Single(Emails.SentTo(email));
    }

    [Fact]
    public async Task Resend_for_unknown_email_is_accepted_silently()
    {
        var email = AuthApi.NewEmail();

        var resend = await _api.PostAsync("resend-verification", new { email });

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Empty(Emails.SentTo(email));
    }

    [Theory]
    [InlineData("en", "Kitaphane verification code")]
    [InlineData("tr", "Kitaphane doğrulama kodu")]
    public async Task Verification_email_uses_request_language(string language, string expectedSubject)
    {
        var email = AuthApi.NewEmail();

        await _api.PostAsync("register", new { name = "X", email, password = "Password123" }, language: language);

        Assert.Equal(expectedSubject, Emails.SentTo(email).Single().Subject);
    }

    // ------------------------------------------------------------ Sign-in and sessions

    [Fact]
    public async Task Login_with_wrong_password_returns_invalid_credentials()
    {
        var session = await _api.CreateAccountAsync();

        var response = await _api.PostAsync("login", new { email = session.Email, password = "WrongPassword1", rememberMe = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Remember_me_controls_cookie_persistence(bool rememberMe, bool expectsExpires)
    {
        var account = await _api.CreateAccountAsync();

        var session = await _api.LoginAsync(account.Email, account.Password, rememberMe);

        Assert.Equal(expectsExpires, session.SetCookieHeader.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Session_cookie_stays_non_persistent_after_refresh()
    {
        var account = await _api.CreateAccountAsync();
        var session = await _api.LoginAsync(account.Email, account.Password, rememberMe: false);

        var refresh = await _api.PostAsync("refresh", refreshCookie: session.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.DoesNotContain("expires=", AuthApi.ReadSetCookie(refresh)!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Protected_endpoint_requires_access_token()
    {
        var response = await _api.GetMeAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHORIZED", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Refresh_rotates_token_and_detects_reuse()
    {
        var session = await _api.CreateAccountAsync();

        var first = await _api.PostAsync("refresh", refreshCookie: session.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = AuthApi.ReadRefreshCookie(first)!;
        Assert.NotEqual(session.RefreshToken, rotated);

        var newAccessToken = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        Assert.Equal(HttpStatusCode.OK, (await _api.GetMeAsync(newAccessToken)).StatusCode);

        // Presenting the old (rotated) token again revokes the whole session family.
        var reuse = await _api.PostAsync("refresh", refreshCookie: session.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await AuthApi.ReadErrorCodeAsync(reuse));

        var afterReuse = await _api.PostAsync("refresh", refreshCookie: rotated);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_without_cookie_is_unauthorized()
    {
        var response = await _api.PostAsync("refresh");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Logout_revokes_refresh_token_and_clears_cookie()
    {
        var session = await _api.CreateAccountAsync();

        var logout = await _api.PostAsync("logout", refreshCookie: session.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", AuthApi.ReadSetCookie(logout)!, StringComparison.OrdinalIgnoreCase);

        var refresh = await _api.PostAsync("refresh", refreshCookie: session.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    // ------------------------------------------------------------ Password reset

    [Fact]
    public async Task Password_can_be_reset_with_emailed_code()
    {
        var session = await _api.CreateAccountAsync();

        var forgot = await _api.PostAsync("forgot-password", new { email = session.Email });
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);

        var verify = await _api.PostAsync("verify-reset-code", new { email = session.Email, code = Emails.LatestCodeFor(session.Email) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        var resetToken = (await verify.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resetToken").GetString();

        var reset = await _api.PostAsync("reset-password", new { email = session.Email, resetToken, newPassword = "NewPassword456" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        // Old sessions are signed out, old password no longer works, new one does.
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.PostAsync("refresh", refreshCookie: session.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _api.PostAsync("login", new { email = session.Email, password = session.Password, rememberMe = false })).StatusCode);
        await _api.LoginAsync(session.Email, "NewPassword456");

        // The reset token is single-use.
        var again = await _api.PostAsync("reset-password", new { email = session.Email, resetToken, newPassword = "Another789" });
        Assert.Equal("INVALID_RESET_TOKEN", await AuthApi.ReadErrorCodeAsync(again));
    }

    [Fact]
    public async Task Reset_code_can_only_be_used_once()
    {
        var session = await _api.CreateAccountAsync();
        await _api.PostAsync("forgot-password", new { email = session.Email });
        var code = Emails.LatestCodeFor(session.Email);

        Assert.Equal(HttpStatusCode.OK, (await _api.PostAsync("verify-reset-code", new { email = session.Email, code })).StatusCode);

        var second = await _api.PostAsync("verify-reset-code", new { email = session.Email, code });
        Assert.Equal("VERIFICATION_CODE_INVALID", await AuthApi.ReadErrorCodeAsync(second));
    }

    [Fact]
    public async Task New_password_must_differ_from_current()
    {
        var session = await _api.CreateAccountAsync();
        await _api.PostAsync("forgot-password", new { email = session.Email });
        var verify = await _api.PostAsync("verify-reset-code", new { email = session.Email, code = Emails.LatestCodeFor(session.Email) });
        var resetToken = (await verify.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("resetToken").GetString();

        var reset = await _api.PostAsync("reset-password", new { email = session.Email, resetToken, newPassword = session.Password });

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        Assert.Equal("PASSWORD_SAME_AS_CURRENT", await AuthApi.ReadErrorCodeAsync(reset));
    }

    [Fact]
    public async Task Forgot_password_for_unknown_email_sends_nothing()
    {
        var email = AuthApi.NewEmail();

        var response = await _api.PostAsync("forgot-password", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(Emails.SentTo(email));
    }

    // ------------------------------------------------------------ Change password

    [Fact]
    public async Task Change_password_requires_correct_current_password()
    {
        var session = await _api.CreateAccountAsync();

        var response = await _api.PostAsync("change-password",
            new { currentPassword = "WrongPassword1", newPassword = "NewPassword456" }, accessToken: session.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_CURRENT_PASSWORD", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Change_password_keeps_current_session_and_ends_others()
    {
        var current = await _api.CreateAccountAsync();
        var other = await _api.LoginAsync(current.Email, current.Password);

        var response = await _api.PostAsync("change-password",
            new { currentPassword = current.Password, newPassword = "NewPassword456" },
            refreshCookie: current.RefreshToken, accessToken: current.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _api.PostAsync("refresh", refreshCookie: current.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.PostAsync("refresh", refreshCookie: other.RefreshToken)).StatusCode);
        await _api.LoginAsync(current.Email, "NewPassword456");
    }
}
