using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Kitaphane.Application.Common;

namespace Kitaphane.IntegrationTests.Infrastructure;

/// <summary>Captures outgoing email instead of sending it.</summary>
public sealed partial class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

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
