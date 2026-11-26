using System.Net.Http.Json;
using Kitaphane.Application.Common.Errors;
using Kitaphane.Application.GoogleBooks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kitaphane.Infrastructure.GoogleBooks;

/// <summary>
/// Calls the Google Books API from the server so the API key never reaches the browser.
/// Results are cached briefly to save quota.
/// </summary>
public sealed class GoogleBooksClient(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<GoogleBooksClient> logger) : IGoogleBooksClient
{
    public const string BaseAddress = "https://www.googleapis.com/books/v1/";
    public const int MaxResults = 20;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<GoogleBookSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var cacheKey = $"google-books:{query.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<GoogleBookSearchResultDto>? cached) && cached is not null)
        {
            return cached;
        }

        var url = $"volumes?q={Uri.EscapeDataString(query)}&maxResults={MaxResults}&printType=books";
        var apiKey = configuration["GOOGLE_BOOKS_API_KEY"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            url += $"&key={Uri.EscapeDataString(apiKey)}";
        }

        GoogleBooksResponse? response;
        try
        {
            using var httpResponse = await httpClient.GetAsync(url, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                // The URL contains the API key, so only the status code is logged.
                logger.LogWarning("Google Books request failed with status {StatusCode}.", (int)httpResponse.StatusCode);
                throw Unavailable();
            }

            response = await httpResponse.Content.ReadFromJsonAsync<GoogleBooksResponse>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException { CancellationToken.IsCancellationRequested: false } or System.Text.Json.JsonException)
        {
            logger.LogWarning("Google Books request failed: {Error}.", exception.GetType().Name);
            throw Unavailable();
        }

        var results = GoogleBooksMapper.Map(response);
        cache.Set(cacheKey, results, CacheDuration);
        return results;
    }

    private static BadGatewayException Unavailable() =>
        new(ErrorCodes.GoogleBooksUnavailable, "Google Books is not available right now.");
}
