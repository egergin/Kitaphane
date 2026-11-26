using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Kitaphane.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kitaphane.Infrastructure.Email;

/// <summary>
/// Sends email through the Resend HTTPS API (https://resend.com/docs/api-reference/emails/send-email).
/// Used in production because the hosting platform blocks outbound SMTP ports; HTTPS (443) is allowed.
/// </summary>
public sealed class ResendEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    public const string BaseAddress = "https://api.resend.com/";

    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var body = new ResendEmailRequest(
            $"{_options.FromName} <{_options.FromEmail}>", [message.To], message.Subject, message.HtmlBody, message.TextBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails") { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new("Bearer", _options.ResendApiKey);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Resend API request failed.");
            throw EmailDeliveryErrors.Failed();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "Resend API rejected the email ({StatusCode}): {Error}",
                    (int)response.StatusCode,
                    error.Length > 500 ? error[..500] : error);
                throw EmailDeliveryErrors.Failed();
            }
        }
    }

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string Text);
}
