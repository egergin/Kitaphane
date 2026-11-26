using System.Net;
using System.Net.Http.Json;
using Kitaphane.API.Middleware;
using Kitaphane.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kitaphane.IntegrationTests;

/// <summary>
/// Production setup: the API only accepts requests from the Cloudflare Pages proxy (shared secret)
/// and rate limits by the client IP the proxy reports.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ProxyGatewayTests(KitaphaneApiFactory factory)
{
    private const string Secret = "proxy-secret-for-tests";

    private HttpClient CreateClient(int authRateLimit = 10000) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(ProxyGatewayMiddleware.SecretKey, Secret);
            builder.UseSetting("AUTH_RATE_LIMIT_PER_MINUTE", authRateLimit.ToString());
        }).CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static HttpRequestMessage Request(HttpMethod method, string path, string? secret, string? clientIp = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        if (secret is not null) request.Headers.Add(ProxyGatewayMiddleware.SecretHeader, secret);
        if (clientIp is not null) request.Headers.Add(ProxyGatewayMiddleware.ClientIpHeader, clientIp);
        return request;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-secret")]
    public async Task Requests_without_the_proxy_secret_are_rejected(string? secret)
    {
        var client = CreateClient();

        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/auth/login", secret, body: new
        {
            email = "someone@example.com",
            password = "Password123",
            rememberMe = false,
        }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Requests_with_the_proxy_secret_reach_the_api()
    {
        var client = CreateClient();

        var response = await client.SendAsync(Request(HttpMethod.Get, "/api/auth/me", Secret));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // passed the gateway, no token
    }

    [Fact]
    public async Task Health_check_does_not_need_the_proxy_secret()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Auth_rate_limit_is_applied_per_client_ip_reported_by_the_proxy()
    {
        var client = CreateClient(authRateLimit: 2);
        object Body() => new { email = AuthApi.NewEmail() };

        async Task<HttpStatusCode> ForgotPassword(string ip) =>
            (await client.SendAsync(Request(HttpMethod.Post, "/api/auth/forgot-password", Secret, ip, Body()))).StatusCode;

        Assert.Equal(HttpStatusCode.Accepted, await ForgotPassword("203.0.113.10"));
        Assert.Equal(HttpStatusCode.Accepted, await ForgotPassword("203.0.113.10"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await ForgotPassword("203.0.113.10"));

        // Another visitor behind the same proxy is not affected.
        Assert.Equal(HttpStatusCode.Accepted, await ForgotPassword("198.51.100.7"));
    }
}
