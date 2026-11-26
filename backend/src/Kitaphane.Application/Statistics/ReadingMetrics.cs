using Kitaphane.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace Kitaphane.Application.Statistics;

/// <summary>
/// Reading figures for a date range, shared by the reading statistics and reading goal progress
/// (docs/database/database-design.md §10). Both range ends are inclusive.
/// </summary>
internal static class ReadingMetrics
{
    /// <summary>COMPLETED books whose FinishDate is in the range.</summary>
    public static IQueryable<Book> CompletedIn(this IQueryable<Book> books, DateOnly start, DateOnly end) =>
        books.Where(book => book.Status == BookStatus.Completed && book.FinishDate >= start && book.FinishDate <= end);

    /// <summary>Current page of READING books started in the range.</summary>
    public static Task<int> ReadingPagesInAsync(
        this IQueryable<Book> books, DateOnly start, DateOnly end, CancellationToken cancellationToken) =>
        books
            .Where(book => book.Status == BookStatus.Reading && book.StartDate >= start && book.StartDate <= end)
            .SumAsync(book => book.CurrentPage, cancellationToken);

    /// <summary>Completed books' page counts plus the current page of books being read.</summary>
    public static async Task<int> PagesReadInAsync(
        this IQueryable<Book> books, DateOnly start, DateOnly end, CancellationToken cancellationToken) =>
        await books.CompletedIn(start, end).SumAsync(book => book.PageCount, cancellationToken)
        + await books.ReadingPagesInAsync(start, end, cancellationToken);
}
