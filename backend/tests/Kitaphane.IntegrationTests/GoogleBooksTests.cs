using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class GoogleBooksTests(KitaphaneApiFactory factory)
{
    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateApiClient();
        var session = await new AuthApi(client, factory.Emails).CreateAccountAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    private const string TwoVolumes = """
        {
          "totalItems": 2,
          "items": [
            {
              "id": "vol-1",
              "volumeInfo": {
                "title": "Suç ve Ceza",
                "authors": ["Fyodor Dostoyevski", "Çev. Mazlum Beyhan"],
                "pageCount": 687,
                "categories": ["Fiction"],
                "industryIdentifiers": [
                  { "type": "ISBN_10", "identifier": "0306406152" },
                  { "type": "ISBN_13", "identifier": "9780306406157" }
                ],
                "imageLinks": {
                  "smallThumbnail": "http://books.google.com/small.jpg",
                  "thumbnail": "http://books.google.com/thumb.jpg"
                }
              }
            },
            {
              "id": "vol-2",
              "volumeInfo": {
                "title": "Suç ve Ceza (Kısaltılmış)",
                "industryIdentifiers": [ { "type": "OTHER", "identifier": "UOM:123" } ]
              }
            },
            { "id": "vol-3", "volumeInfo": { } }
          ]
        }
        """;

    [Fact]
    public async Task Search_maps_google_volumes_to_suggestions()
    {
        factory.GoogleBooks.Respond("suç ve ceza mapping", TwoVolumes);
        var client = await SignedInClientAsync();

        var response = await client.GetAsync("/api/google-books/search?query=su%C3%A7%20ve%20ceza%20mapping");
        var results = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, results.GetArrayLength()); // the volume without a title is skipped

        var first = results[0];
        Assert.Equal("vol-1", first.GetProperty("googleBooksId").GetString());
        Assert.Equal("Suç ve Ceza", first.GetProperty("title").GetString());
        Assert.Equal("Fyodor Dostoyevski, Çev. Mazlum Beyhan", first.GetProperty("author").GetString());
        Assert.Equal(687, first.GetProperty("pageCount").GetInt32());
        Assert.Equal("9780306406157", first.GetProperty("isbn").GetString()); // ISBN-13 preferred
        Assert.Equal("https://books.google.com/thumb.jpg", first.GetProperty("coverImage").GetString()); // https
        Assert.False(first.TryGetProperty("categories", out _));

        var second = results[1];
        Assert.Equal(JsonValueKind.Null, second.GetProperty("author").ValueKind);
        Assert.Equal(JsonValueKind.Null, second.GetProperty("pageCount").ValueKind);
        Assert.Equal(JsonValueKind.Null, second.GetProperty("isbn").ValueKind);
        Assert.Equal(JsonValueKind.Null, second.GetProperty("coverImage").ValueKind);
    }

    [Fact]
    public async Task Isbn_search_normalizes_isbn_and_sends_api_key()
    {
        factory.GoogleBooks.Respond("isbn:9780321822482", TwoVolumes);
        var client = await SignedInClientAsync();

        var response = await client.GetAsync("/api/google-books/isbn/978-0-321-82248-2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = factory.GoogleBooks.Requests.Last(uri => uri.Query.Contains("isbn%3A9780321822482"));
        Assert.Contains("key=test-google-key", request.Query);
        Assert.Contains("maxResults=20", request.Query);
    }

    [Fact]
    public async Task Invalid_isbn_is_rejected_without_calling_google()
    {
        var client = await SignedInClientAsync();
        var before = factory.GoogleBooks.Requests.Count;

        var response = await client.GetAsync("/api/google-books/isbn/12345");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty("isbn", out _));
        Assert.Equal(before, factory.GoogleBooks.Requests.Count);
    }

    [Fact]
    public async Task Empty_query_is_rejected()
    {
        var client = await SignedInClientAsync();

        var response = await client.GetAsync("/api/google-books/search?query=%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, """{ "error": { "code": 429 } }""")]
    [InlineData(HttpStatusCode.InternalServerError, "oops")]
    [InlineData(HttpStatusCode.OK, "not json")]
    public async Task Google_errors_become_bad_gateway(HttpStatusCode status, string body)
    {
        var query = $"failing {status} {body.Length}";
        factory.GoogleBooks.Respond(query, body, status);
        var client = await SignedInClientAsync();

        var response = await client.GetAsync($"/api/google-books/search?query={Uri.EscapeDataString(query)}");
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("GOOGLE_BOOKS_UNAVAILABLE", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Repeated_search_is_served_from_cache()
    {
        factory.GoogleBooks.Respond("cached query", TwoVolumes);
        var client = await SignedInClientAsync();

        await client.GetAsync("/api/google-books/search?query=cached%20query");
        await client.GetAsync("/api/google-books/search?query=Cached%20Query");

        Assert.Single(factory.GoogleBooks.Requests, uri => uri.Query.Contains("cached%20query", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Google_books_requires_authentication()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/google-books/search?query=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
