namespace Kitaphane.Application.GoogleBooks;

/// <summary>
/// A Google Books result offered to the user before saving. Categories/subjects are deliberately not included.
/// Missing values are null; the user completes them in the form.
/// </summary>
public sealed record GoogleBookSearchResultDto(
    string GoogleBooksId,
    string Title,
    string? Author,
    int? PageCount,
    string? Isbn,
    string? CoverImage);

public sealed record GoogleBooksSearchQuery
{
    public const int MaxQueryLength = 200;

    public string Query { get; init; } = string.Empty;
}

public interface IGoogleBooksClient
{
    /// <summary>Searches Google Books. <paramref name="query"/> is a Google Books query, e.g. a title or "isbn:...".</summary>
    Task<IReadOnlyList<GoogleBookSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken);
}
