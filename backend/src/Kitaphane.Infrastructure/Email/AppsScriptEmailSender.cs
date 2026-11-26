using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitaphane.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kitaphane.Infrastructure.Email;

/// <summary>
/// Sends email through a Google Apps Script web app (deploy/apps-script/Code.gs) that sends it from the
/// Gmail account the script belongs to. Used in production when there is no own domain for an email
/// provider; it runs over HTTPS (443) because the host blocks outbound SMTP ports.
/// </summary>
/// <remarks>
/// Apps Script cannot set HTTP status codes or read request headers: the shared secret travels in the
/// JSON body and the script answers <c>{"ok":true}</c> or <c>{"ok":false,"error":"..."}</c>. The /exec URL
/// answers a POST with a redirect to the result, which HttpClient follows as a GET.
/// </remarks>
public sealed class AppsScriptEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<AppsScriptEmailSender> logger) : IEmailSender
{
    public const int MinimumSecretLength = 32;

    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var body = new AppsScriptEmailRequest(
            _options.AppsScriptSecret!, message.To, _options.FromName, message.Subject, message.HtmlBody, message.TextBody);

        int statusCode;
        string content;
        try
        {
            using var response = await httpClient.PostAsJsonAsync(_options.AppsScriptUrl, body, cancellationToken);
            statusCode = (int)response.StatusCode;
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Apps Script email request failed.");
            throw EmailDeliveryErrors.Failed();
        }

        if (statusCode is < 200 or >= 300)
        {
            logger.LogError("Apps Script email request failed ({StatusCode}).", statusCode);
            throw EmailDeliveryErrors.Failed();
        }

        var result = TryParse(content);
        if (result is not { Ok: true })
        {
            // A non-JSON answer usually means the web app is not deployed with access "Anyone".
            logger.LogError("Apps Script did not send the email: {Error}", result?.Error ?? "unexpected response");
            throw EmailDeliveryErrors.Failed();
        }
    }

    private static AppsScriptEmailResponse? TryParse(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<AppsScriptEmailResponse>(content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record AppsScriptEmailRequest(
        [property: JsonPropertyName("secret")] string Secret,
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("fromName")] string FromName,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string Text);

    private sealed record AppsScriptEmailResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("error")] string? Error);
}
