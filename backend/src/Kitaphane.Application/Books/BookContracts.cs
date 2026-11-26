using Kitaphane.Domain.Books;

namespace Kitaphane.Application.Books;

/// <summary>Home page card. Reading percentage is calculated by the client from CurrentPage / PageCount.</summary>
public sealed record BookListItemDto(
    Guid Id,
    string Title,
    string Author,
    string? CoverImage,
    int CurrentPage,
    int PageCount);

/// <summary>Row of the "Okuduğum Kitaplar" list.</summary>
public sealed record CompletedBookDto(
    Guid Id,
    string Title,
    string Author,
    string? CoverImage,
    string? Isbn,
    int PageCount,
    int? Rating,
    DateOnly? FinishDate);

/// <summary>Status-only change, e.g. taking a book off the "read" list.</summary>
public sealed record UpdateBookStatusRequest(BookStatus Status);

public sealed record BookDetailDto(
    Guid Id,
    string Title,
    string Author,
    string? Isbn,
    int PageCount,
    int CurrentPage,
    BookStatus Status,
    string? CoverImage,
    int? Rating,
    string? Notes,
    DateOnly? StartDate,
    DateOnly? FinishDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Fields shared by create and update requests.</summary>
public interface IBookRequest
{
    string Title { get; }
    string Author { get; }
    int PageCount { get; }
    int CurrentPage { get; }
    BookStatus Status { get; }
    string? CoverImage { get; }
    string? Isbn { get; }
    int? Rating { get; }
    string? Notes { get; }
    DateOnly? StartDate { get; }
    DateOnly? FinishDate { get; }
}

/// <summary>New book. Status defaults to TO_READ and CurrentPage to 0.</summary>
public sealed record CreateBookRequest(
    string Title,
    string Author,
    int PageCount,
    int CurrentPage = 0,
    BookStatus Status = BookStatus.ToRead,
    string? CoverImage = null,
    string? Isbn = null,
    int? Rating = null,
    string? Notes = null,
    DateOnly? StartDate = null,
    DateOnly? FinishDate = null) : IBookRequest;

/// <summary>Full update of a book (PUT).</summary>
public sealed record UpdateBookRequest(
    string Title,
    string Author,
    int PageCount,
    int CurrentPage,
    BookStatus Status,
    string? CoverImage,
    string? Isbn,
    int? Rating,
    string? Notes,
    DateOnly? StartDate,
    DateOnly? FinishDate) : IBookRequest;

public sealed record GetBooksQuery
{
    public const int DefaultPageSize = 18;
    public static readonly int[] AllowedPageSizes = [18, 36, 72, 108];

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public const int MaxSearchLength = 100;

    /// <summary>Optional text matched (case-insensitive) against title and author.</summary>
    public string? Search { get; init; }
}
