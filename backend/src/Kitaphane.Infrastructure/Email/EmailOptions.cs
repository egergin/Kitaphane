using Microsoft.Extensions.Configuration;

namespace Kitaphane.Infrastructure.Email;

/// <summary>
/// Email settings. The host blocks outbound SMTP ports in production, so email goes over HTTPS there:
/// <list type="number">
/// <item><c>APPS_SCRIPT_EMAIL_URL</c> + <c>APPS_SCRIPT_EMAIL_SECRET</c> → a Google Apps Script web app that
/// sends from the Gmail account it belongs to (deploy/apps-script/Code.gs).</item>
/// <item><c>RESEND_API_KEY</c> → the Resend HTTPS API.</item>
/// <item>Otherwise SMTP (local development: Mailpit).</item>
/// </list>
/// </summary>
public sealed class EmailOptions
{
    public string? AppsScriptUrl { get; set; }

    public string? AppsScriptSecret { get; set; }

    public string? ResendApiKey { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Kitaphane";

    public bool UsesAppsScript => !string.IsNullOrWhiteSpace(AppsScriptUrl);

    public bool UsesResendApi => !UsesAppsScript && !string.IsNullOrWhiteSpace(ResendApiKey);

    public static void Bind(EmailOptions options, IConfiguration configuration)
    {
        options.AppsScriptUrl = configuration["APPS_SCRIPT_EMAIL_URL"];
        options.AppsScriptSecret = configuration["APPS_SCRIPT_EMAIL_SECRET"];
        options.ResendApiKey = configuration["RESEND_API_KEY"];
        options.Host = configuration["SMTP_HOST"] ?? string.Empty;
        options.Port = int.TryParse(configuration["SMTP_PORT"], out var port) ? port : options.Port;
        options.Username = configuration["SMTP_USERNAME"];
        options.Password = configuration["SMTP_PASSWORD"];
        options.FromEmail = configuration["SMTP_FROM_EMAIL"] ?? string.Empty;
        options.FromName = configuration["SMTP_FROM_NAME"] is { Length: > 0 } name ? name : options.FromName;
    }
}
