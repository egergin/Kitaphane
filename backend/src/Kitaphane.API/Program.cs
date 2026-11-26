using Kitaphane.API.Extensions;
using Kitaphane.API.Middleware;
using Kitaphane.Application;
using Kitaphane.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddApi(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment() && string.IsNullOrEmpty(app.Configuration[ProxyGatewayMiddleware.SecretKey]))
{
    throw new InvalidOperationException(
        $"Environment variable '{ProxyGatewayMiddleware.SecretKey}' is required outside Development (see docs/deployment/deployment.md).");
}

// Cloud Run terminates TLS and forwards plain HTTP with X-Forwarded-Proto. Cloud Run is the only
// ingress, so the header is trusted from any address. The client IP comes from ProxyGatewayMiddleware.
var forwardedHeaders = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto };
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

// Errors are written by GlobalExceptionHandler (IExceptionHandler); the empty pipeline is only a fallback.
app.UseExceptionHandler(_ => { });
app.UseStatusCodePages(StatusCodeResponses.WriteAsync);
app.UseMiddleware<ProxyGatewayMiddleware>();

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();

    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Kitaphane API");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRequestLocalization();
app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
