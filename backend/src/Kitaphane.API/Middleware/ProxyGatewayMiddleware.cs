using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Kitaphane.API.Middleware;

/// <summary>
/// In production the API is only reached through the Cloudflare Pages proxy (frontend/functions/api).
/// The proxy sends a shared secret and the browser's IP address; requests without the secret are
/// rejected, so the public backend URL cannot be used directly and the client IP header cannot be
/// spoofed. The real client IP is used by the auth rate limiter.
/// Disabled when <c>PROXY_SHARED_SECRET</c> is not configured (local development).
/// </summary>
public sealed class ProxyGatewayMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public const string SecretKey = "PROXY_SHARED_SECRET";
    public const string SecretHeader = "X-Kitaphane-Proxy-Secret";
    public const string ClientIpHeader = "X-Kitaphane-Client-IP";

    /// <summary>The host's health checks and uptime monitors may call the health endpoint directly.</summary>
    private const string HealthPath = "/api/health";

    private readonly byte[]? _secret = string.IsNullOrEmpty(configuration[SecretKey])
        ? null
        : Encoding.UTF8.GetBytes(configuration[SecretKey]!);

    public async Task InvokeAsync(HttpContext context)
    {
        if (_secret is null || context.Request.Path.StartsWithSegments(HealthPath))
        {
            await next(context);
            return;
        }

        var provided = Encoding.UTF8.GetBytes(context.Request.Headers[SecretHeader].ToString());
        if (!CryptographicOperations.FixedTimeEquals(provided, _secret))
        {
            // The body is written by the status code pages handler (FORBIDDEN).
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (IPAddress.TryParse(context.Request.Headers[ClientIpHeader].ToString(), out var clientIp))
        {
            context.Connection.RemoteIpAddress = clientIp;
        }

        // The secret is only meant for this check.
        context.Request.Headers.Remove(SecretHeader);
        await next(context);
    }
}
