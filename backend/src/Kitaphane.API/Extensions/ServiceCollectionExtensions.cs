using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Kitaphane.API.Auth;
using Kitaphane.API.Filters;
using Kitaphane.API.Middleware;
using Kitaphane.API.OpenApi;
using Kitaphane.Application.Common;
using Kitaphane.Infrastructure.Identity;
using Kitaphane.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Kitaphane.API.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";
    public const string AuthRateLimitPolicy = "auth";

    private const string CorsOriginsKey = "CORS_ORIGINS";
    private const string AuthRateLimitKey = "AUTH_RATE_LIMIT_PER_MINUTE";
    private const int DefaultAuthRateLimitPerMinute = 20;

    public static readonly string[] SupportedLanguages = ["tr", "en"];

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddJsonOptions(options =>
            {
                // Enums are exchanged as UPPER_SNAKE_CASE strings, e.g. ToRead -> "TO_READ".
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
            });

        // Model binding / JSON parsing errors use the same error format as FluentValidation errors.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key.TrimStart('$', '.')),
                        entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

                return new BadRequestObjectResult(GlobalExceptionHandler.ValidationErrorResponse(errors));
            };
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        AddAuthentication(services);
        AddRateLimiting(services, configuration);
        AddCors(services, configuration);

        // Emails are sent in the language of the request (Accept-Language: tr | en). Default: Turkish.
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedLanguages.Select(language => new CultureInfo(language)).ToList();
            options.DefaultRequestCulture = new RequestCulture(SupportedLanguages[0]);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services;
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TokenService.CreateSigningKey(jwt.Secret),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                };
            });

        // Every endpoint requires an authenticated user unless it opts out with [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = int.TryParse(configuration[AuthRateLimitKey], out var limit) && limit > 0
            ? limit
            : DefaultAuthRateLimitPerMinute;

        services.AddRateLimiter(options =>
        {
            // The body is written by the status code pages handler (RATE_LIMITED).
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            var origins = (configuration[CorsOriginsKey] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            options.AddPolicy(FrontendCorsPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });
    }
}
