using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Kitaphane.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for the Google Books API. Tests register a response per query text; unknown queries return no items.
/// </summary>
public sealed class FakeGoogleBooksApi
{
    private readonly ConcurrentDictionary<string, (HttpStatusCode Status, string Body)> _responses = new();

    public ConcurrentQueue<Uri> Requests { get; } = new();

    public void Respond(string query, string json, HttpStatusCode status = HttpStatusCode.OK) =>
        _responses[query] = (status, json);

    public HttpMessageHandler CreateHandler() => new Handler(this);

    private sealed class Handler(FakeGoogleBooksApi api) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            api.Requests.Enqueue(request.RequestUri!);

            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query)["q"] ?? string.Empty;
            var (status, body) = api._responses.TryGetValue(query, out var response)
                ? response
                : (HttpStatusCode.OK, """{ "totalItems": 0 }""");

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
