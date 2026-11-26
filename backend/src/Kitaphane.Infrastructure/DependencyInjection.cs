using Kitaphane.Application.Auth;
using Kitaphane.Application.Common;
using Kitaphane.Application.GoogleBooks;
using Kitaphane.Application.Profile;
using Kitaphane.Infrastructure.Email;
using Kitaphane.Infrastructure.GoogleBooks;
using Kitaphane.Infrastructure.Identity;
using Kitaphane.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kitaphane.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringKey = "DATABASE_CONNECTION_STRING";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        AddPersistence(services);
        AddIdentity(services);
        AddEmail(services);
        AddGoogleBooks(services);

        return services;
    }

    private static void AddPersistence(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration[ConnectionStringKey];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{ConnectionStringKey}' is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                // Neon suspends idle databases; the first connection after a pause may fail transiently.
                .EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null));
        });

        services.AddScoped<IAppDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .Configure<IConfiguration>(JwtOptions.Bind)
            .Validate(
                options => options.Secret.Length >= JwtOptions.MinimumSecretLength,
                $"JWT_SECRET must be at least {JwtOptions.MinimumSecretLength} characters.")
            .Validate(options => options.AccessTokenMinutes > 0 && options.RefreshTokenDays > 0,
                "JWT token lifetimes must be positive.")
            .ValidateOnStart();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty; // UserName = email; allow any email characters.

                // Mirrors the FluentValidation password rule: 8+ characters with a letter and a digit.
                options.Password.RequiredLength = AuthRules.PasswordMinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                // Accounts are not locked after failed sign-ins; auth endpoints are rate limited instead.
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<SecretHasher>();
        services.AddSingleton<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
    }

    private static void AddEmail(IServiceCollection services)
    {
        services.AddOptions<EmailOptions>()
            .Configure<IConfiguration>(EmailOptions.Bind)
            .Validate(
                options => options.UsesAppsScript || options.UsesResendApi || !string.IsNullOrWhiteSpace(options.Host),
                "One of APPS_SCRIPT_EMAIL_URL, RESEND_API_KEY or SMTP_HOST must be configured.")
            .Validate(
                options => !options.UsesAppsScript
                    || (Uri.TryCreate(options.AppsScriptUrl, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps),
                "APPS_SCRIPT_EMAIL_URL must be an https URL.")
            .Validate(
                options => !options.UsesAppsScript || (options.AppsScriptSecret?.Length ?? 0) >= AppsScriptEmailSender.MinimumSecretLength,
                $"APPS_SCRIPT_EMAIL_SECRET must be at least {AppsScriptEmailSender.MinimumSecretLength} characters.")
            // With Apps Script the sender address is the Gmail account the script runs as.
            .Validate(
                options => options.UsesAppsScript || !string.IsNullOrWhiteSpace(options.FromEmail),
                "SMTP_FROM_EMAIL is not configured.")
            .ValidateOnStart();

        // APPS_SCRIPT_EMAIL_URL → Gmail via Apps Script, RESEND_API_KEY → Resend (both HTTPS, production),
        // otherwise SMTP (local Mailpit).
        services.AddHttpClient<AppsScriptEmailSender>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri(ResendEmailSender.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<IEmailSender>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
            if (options.UsesAppsScript) return serviceProvider.GetRequiredService<AppsScriptEmailSender>();
            if (options.UsesResendApi) return serviceProvider.GetRequiredService<ResendEmailSender>();
            return serviceProvider.GetRequiredService<SmtpEmailSender>();
        });
    }

    private static void AddGoogleBooks(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<IGoogleBooksClient, GoogleBooksClient>(client =>
        {
            client.BaseAddress = new Uri(GoogleBooksClient.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}
