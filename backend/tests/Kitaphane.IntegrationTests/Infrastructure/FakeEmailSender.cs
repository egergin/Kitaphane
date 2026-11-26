using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Kitaphane.Application.Common;
using Kitaphane.Application.Common.Errors;

namespace Kitaphane.IntegrationTests.Infrastructure;

/// <summary>Captures outgoing email instead of sending it.</summary>
public sealed partial class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();
    private readonly ConcurrentDictionary<string, bool> _failingRecipients = new(StringComparer.OrdinalIgnoreCase);

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (_failingRecipients.ContainsKey(message.To))
        {
            throw new ServiceUnavailableException(ErrorCodes.EmailDeliveryFailed, "Simulated delivery failure.");
        }

        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>Makes delivery to <paramref name="email"/> fail like an unreachable email provider.</summary>
    public void FailDeliveryTo(string email) => _failingRecipients[email] = true;

    public void RestoreDeliveryTo(string email) => _failingRecipients.TryRemove(email, out _);

    public IReadOnlyList<EmailMessage> SentTo(string email) =>
        _messages.Where(message => string.Equals(message.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

    public string LatestCodeFor(string email)
    {
        var message = SentTo(email).LastOrDefault()
            ?? throw new InvalidOperationException($"No email was sent to {email}.");
        return CodePattern().Match(message.TextBody).Value;
    }

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex CodePattern();
}
