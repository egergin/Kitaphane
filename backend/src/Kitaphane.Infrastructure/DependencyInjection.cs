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
        services.AddOptions<SmtpOptions>()
            .Configure<IConfiguration>(SmtpOptions.Bind)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "SMTP_HOST is not configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.FromEmail), "SMTP_FROM_EMAIL is not configured.")
            .ValidateOnStart();

        services.AddScoped<IEmailSender, SmtpEmailSender>();
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
