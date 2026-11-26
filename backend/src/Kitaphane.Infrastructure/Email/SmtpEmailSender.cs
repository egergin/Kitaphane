using Kitaphane.Application.Common;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Kitaphane.Infrastructure.Email;

/// <summary>
/// Sends email over SMTP. Used in local development (Mailpit, no TLS). Failures surface as
/// EMAIL_DELIVERY_FAILED (503) instead of a generic server error.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    /// <summary>Gives up quickly when the server is unreachable (e.g. the port is blocked).</summary>
    private const int TimeoutMilliseconds = 15_000;

    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient { Timeout = TimeoutMilliseconds };

        try
        {
            // Port 465 → implicit TLS, 587 → STARTTLS, Mailpit (1025) → plain.
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, cancellationToken);

            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(FormatOptions.Default, new SendRequest(mimeMessage), cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "SMTP delivery to {Host}:{Port} failed.", _options.Host, _options.Port);
            throw EmailDeliveryErrors.Failed();
        }
    }
}
