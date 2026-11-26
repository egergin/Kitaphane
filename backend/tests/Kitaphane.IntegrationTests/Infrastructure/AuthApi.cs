using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kitaphane.IntegrationTests.Infrastructure;

/// <summary>Small helper around the auth endpoints that tracks the refresh token cookie manually.</summary>
public sealed class AuthApi(HttpClient client, FakeEmailSender emails)
{
    public const string RefreshCookieName = "kitaphane_rt";

    public HttpClient Client => client;

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null, string? refreshCookie = null, string? accessToken = null, string? language = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/auth/{path}")
        {
            Content = body is null ? null : JsonContent.Create(body),
        };

        if (refreshCookie is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshCookie}");
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (language is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(language);
        }

        return client.SendAsync(request);
    }

    public Task<HttpResponseMessage> GetMeAsync(string? accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client.SendAsync(request);
    }

    /// <summary>Registers and verifies a new account. Returns the signed-in session.</summary>
    public async Task<Session> CreateAccountAsync(string? email = null, string password = "Password123", string name = "Test Kullanıcı")
    {
        email ??= NewEmail();

        var register = await PostAsync("register", new { name, email, password });
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var verify = await PostAsync("verify-email", new { email, code = emails.LatestCodeFor(email) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        return await Session.FromResponseAsync(verify, email, password);
    }

    public async Task<Session> LoginAsync(string email, string password, bool rememberMe = true)
    {
        var response = await PostAsync("login", new { email, password, rememberMe });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await Session.FromResponseAsync(response, email, password);
    }

    public static string? ReadRefreshCookie(HttpResponseMessage response) =>
        ReadSetCookie(response)?.Split(';')[0].Split('=', 2)[1] is { Length: > 0 } value ? value : null;

    public static string? ReadSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal))
            : null;

    public static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}

public sealed record Session(string Email, string Password, string AccessToken, string RefreshToken, string SetCookieHeader)
{
    public static async Task<Session> FromResponseAsync(HttpResponseMessage response, string email, string password)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var setCookie = AuthApi.ReadSetCookie(response) ?? throw new InvalidOperationException("Refresh cookie was not set.");

        return new Session(
            email,
            password,
            body.GetProperty("accessToken").GetString()!,
            AuthApi.ReadRefreshCookie(response)!,
            setCookie);
    }
}
