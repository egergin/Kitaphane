using Microsoft.Extensions.Configuration;

namespace Kitaphane.Infrastructure.Email;

/// <summary>
/// Email settings. With <c>RESEND_API_KEY</c> email goes through the Resend HTTPS API (production:
/// the host blocks outbound SMTP ports); otherwise over SMTP (local development: Mailpit).
/// </summary>
public sealed class EmailOptions
{
    public string? ResendApiKey { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Kitaphane";

    public bool UsesResendApi => !string.IsNullOrWhiteSpace(ResendApiKey);

    public static void Bind(EmailOptions options, IConfiguration configuration)
    {
        options.ResendApiKey = configuration["RESEND_API_KEY"];
        options.Host = configuration["SMTP_HOST"] ?? string.Empty;
        options.Port = int.TryParse(configuration["SMTP_PORT"], out var port) ? port : options.Port;
        options.Username = configuration["SMTP_USERNAME"];
        options.Password = configuration["SMTP_PASSWORD"];
        options.FromEmail = configuration["SMTP_FROM_EMAIL"] ?? string.Empty;
        options.FromName = configuration["SMTP_FROM_NAME"] is { Length: > 0 } name ? name : options.FromName;
    }
}
