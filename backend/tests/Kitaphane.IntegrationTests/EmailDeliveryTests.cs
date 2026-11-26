using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.Application.Common;
using Kitaphane.Application.Common.Errors;
using Kitaphane.Infrastructure;
using Kitaphane.Infrastructure.Email;
using Kitaphane.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kitaphane.IntegrationTests;

/// <summary>
/// Email delivery failures (e.g. the provider is unreachable) are reported as EMAIL_DELIVERY_FAILED
/// and never leave the user stuck in the resend cooldown.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class EmailDeliveryTests(KitaphaneApiFactory factory)
{
    private readonly AuthApi _api = new(factory.CreateApiClient(), factory.Emails);
    private FakeEmailSender Emails => factory.Emails;

    [Fact]
    public async Task Failed_verification_email_is_reported_and_can_be_retried_at_once()
    {
        var email = AuthApi.NewEmail();
        Emails.FailDeliveryTo(email);

        var failed = await _api.PostAsync("register", new { name = "Ayşe", email, password = "Password123" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal("EMAIL_DELIVERY_FAILED", await AuthApi.ReadErrorCodeAsync(failed));

        // Retrying right away (inside the normal 60 s cooldown) sends a fresh code.
        Emails.RestoreDeliveryTo(email);
        var retry = await _api.PostAsync("register", new { name = "Ayşe", email, password = "Password123" });
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);

        var verify = await _api.PostAsync("verify-email", new { email, code = Emails.LatestCodeFor(email) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
    }

    [Fact]
    public async Task Failed_resend_is_reported_and_can_be_retried_at_once()
    {
        var email = AuthApi.NewEmail();
        Emails.FailDeliveryTo(email);
        await _api.PostAsync("register", new { name = "Ali", email, password = "Password123" }); // 503, cooldown cleared

        var failedResend = await _api.PostAsync("resend-verification", new { email });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failedResend.StatusCode);
        Assert.Equal("EMAIL_DELIVERY_FAILED", await AuthApi.ReadErrorCodeAsync(failedResend));

        Emails.RestoreDeliveryTo(email);
        var resend = await _api.PostAsync("resend-verification", new { email });
        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Single(Emails.SentTo(email));
    }

    [Fact]
    public async Task Failed_password_reset_email_is_not_revealed_and_can_be_retried_at_once()
    {
        var session = await _api.CreateAccountAsync();
        Emails.FailDeliveryTo(session.Email);

        var failed = await _api.PostAsync("forgot-password", new { email = session.Email });
        Assert.Equal(HttpStatusCode.Accepted, failed.StatusCode); // same answer as for unknown emails

        Emails.RestoreDeliveryTo(session.Email);
        var retry = await _api.PostAsync("forgot-password", new { email = session.Email });
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);

        var verify = await _api.PostAsync("verify-reset-code", new { email = session.Email, code = Emails.LatestCodeFor(session.Email) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
    }

    [Fact]
    public void Resend_api_is_used_when_its_key_is_configured()
    {
        static IEmailSender Resolve(Dictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var services = new ServiceCollection().AddLogging().AddSingleton<IConfiguration>(configuration);
            services.AddInfrastructure();
            return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IEmailSender>();
        }

        Assert.IsType<ResendEmailSender>(Resolve(new() { ["RESEND_API_KEY"] = "re_test", ["SMTP_FROM_EMAIL"] = "a@b.c" }));
        Assert.IsType<SmtpEmailSender>(Resolve(new() { ["SMTP_HOST"] = "localhost", ["SMTP_FROM_EMAIL"] = "a@b.c" }));
    }
}

/// <summary>ResendEmailSender against a fake HTTP handler (no real email is sent).</summary>
public sealed class ResendEmailSenderTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return await respond(request);
        }
    }

    private static ResendEmailSender CreateSender(FakeHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri(ResendEmailSender.BaseAddress) },
            Options.Create(new EmailOptions { ResendApiKey = "re_test_key", FromEmail = "onboarding@resend.dev", FromName = "Kitaphane" }),
            NullLogger<ResendEmailSender>.Instance);

    private static readonly EmailMessage Message = new("okur@example.com", "Kodunuz", "Kod: 123456", "<p>Kod: 123456</p>");

    [Fact]
    public async Task Posts_the_email_to_the_resend_api()
    {
        var handler = new FakeHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = "email-id" }),
        }));

        await CreateSender(handler).SendAsync(Message);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.resend.com/emails", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer re_test_key", handler.Request.Headers.Authorization!.ToString());
        var body = JsonDocument.Parse(handler.Body!).RootElement;
        Assert.Equal("Kitaphane <onboarding@resend.dev>", body.GetProperty("from").GetString());
        Assert.Equal(["okur@example.com"], body.GetProperty("to").EnumerateArray().Select(to => to.GetString()));
        Assert.Equal("Kodunuz", body.GetProperty("subject").GetString());
        Assert.Equal("<p>Kod: 123456</p>", body.GetProperty("html").GetString());
        Assert.Equal("Kod: 123456", body.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Rejected_email_becomes_email_delivery_failed()
    {
        var handler = new FakeHandler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)422)
        {
            Content = new StringContent("""{"message":"You can only send testing emails to your own email address."}"""),
        }));

        var error = await Assert.ThrowsAsync<ServiceUnavailableException>(() => CreateSender(handler).SendAsync(Message));

        Assert.Equal("EMAIL_DELIVERY_FAILED", error.Code);
    }

    [Fact]
    public async Task Unreachable_api_becomes_email_delivery_failed()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Connection refused"));

        var error = await Assert.ThrowsAsync<ServiceUnavailableException>(() => CreateSender(handler).SendAsync(Message));

        Assert.Equal("EMAIL_DELIVERY_FAILED", error.Code);
    }
}
