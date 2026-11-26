using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ApiFoundationTests(KitaphaneApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateApiClient();

    [Fact]
    public async Task Health_endpoint_reports_healthy_database()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/test-errors/not-found", HttpStatusCode.NotFound, "BOOK_NOT_FOUND", "Book not found.")]
    [InlineData("/api/test-errors/conflict", HttpStatusCode.Conflict, "TAG_EXISTS", "Tag already exists.")]
    [InlineData("/api/test-errors/unexpected", HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")]
    // Anonymous requests to unknown routes hit the "authenticated by default" policy first.
    [InlineData("/api/does-not-exist", HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Authentication is required.")]
    public async Task Errors_use_the_common_format(string url, HttpStatusCode expectedStatus, string expectedCode, string expectedMessage)
    {
        var response = await _client.GetAsync(url);
        var body = await ReadJsonAsync(response);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedCode, body.GetProperty("code").GetString());
        Assert.Equal(expectedMessage, body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Unknown_route_returns_not_found_for_signed_in_user()
    {
        var session = await new AuthApi(_client, factory.Emails).CreateAccountAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/does-not-exist");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);

        var response = await _client.SendAsync(request);
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Validation_errors_include_camel_case_field_errors()
    {
        var response = await _client.GetAsync("/api/test-errors/validation");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.Equal(
            "PageCount must be greater than 0.",
            body.GetProperty("errors").GetProperty("pageCount")[0].GetString());
    }

    [Fact]
    public async Task Invalid_json_body_returns_validation_error_format()
    {
        var response = await _client.PostAsJsonAsync("/api/test-errors/model", new { pageCount = "not-a-number" });
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty("pageCount", out _));
    }

    [Fact]
    public async Task Cors_allows_configured_frontend_origin_with_credentials()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task OpenApi_document_is_available_in_development()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
