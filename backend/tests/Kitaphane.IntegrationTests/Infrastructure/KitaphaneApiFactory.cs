using Kitaphane.Application.Common;
using Kitaphane.Application.GoogleBooks;
using Kitaphane.Infrastructure.GoogleBooks;
using Kitaphane.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Kitaphane.IntegrationTests.Infrastructure;

/// <summary>
/// Starts the API against a throwaway PostgreSQL container, with email captured in memory.
/// </summary>
public sealed class KitaphaneApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public FakeEmailSender Emails { get; } = new();

    public FakeGoogleBooksApi GoogleBooks { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(DependencyInjection.ConnectionStringKey, _postgres.GetConnectionString());
        builder.UseSetting("JWT_SECRET", "integration-tests-secret-key-0123456789-abcdefghij");
        builder.UseSetting("SMTP_HOST", "localhost");
        builder.UseSetting("SMTP_FROM_EMAIL", "tests@kitaphane.local");
        builder.UseSetting("AUTH_RATE_LIMIT_PER_MINUTE", "10000");
        builder.UseSetting("GOOGLE_BOOKS_API_KEY", "test-google-key");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            services.AddHttpClient<IGoogleBooksClient, GoogleBooksClient>()
                .ConfigurePrimaryHttpMessageHandler(GoogleBooks.CreateHandler);
            services.AddControllers().AddApplicationPart(typeof(KitaphaneApiFactory).Assembly);
        });
    }

    /// <summary>Client without automatic cookie handling, so tests control the refresh token cookie explicitly.</summary>
    public HttpClient CreateApiClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    public Task InitializeAsync() => _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<KitaphaneApiFactory>
{
    public const string Name = "Api";
}
