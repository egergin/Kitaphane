using Kitaphane.Application.Common;
using Kitaphane.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace Kitaphane.Application.Statistics;

/// <summary>
/// Statistics computed from the current user's books (there is no statistics table).
/// Formulas: docs/database/database-design.md §10.
/// </summary>
public sealed class StatisticsService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    /// <summary>Supported periods: a window of whole months ending today.</summary>
    public static readonly IReadOnlyDictionary<string, int> Periods = new Dictionary<string, int>
    {
        ["1m"] = 1,
        ["6m"] = 6,
        ["1y"] = 12,
    };

    public async Task<StatisticsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var counts = await OwnedBooks()
            .GroupBy(book => book.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        var completed = counts.GetValueOrDefault(BookStatus.Completed);
        var reading = counts.GetValueOrDefault(BookStatus.Reading);
        var toRead = counts.GetValueOrDefault(BookStatus.ToRead);

        return new StatisticsOverviewDto(completed + reading + toRead, completed, reading, toRead);
    }

    public async Task<ReadingStatisticsDto> GetReadingAsync(ReadingStatisticsQuery query, CancellationToken cancellationToken)
    {
        var end = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var start = end.AddMonths(-Periods[query.Period]);

        var completed = await OwnedBooks()
            .CompletedIn(start, end)
            .Select(book => new { book.Author, book.PageCount })
            .ToListAsync(cancellationToken);

        var readingPages = await OwnedBooks().ReadingPagesInAsync(start, end, cancellationToken);

        var completedPages = completed.Sum(book => book.PageCount);
        var average = completed.Count == 0
            ? 0
            : (int)Math.Round((double)completedPages / completed.Count, MidpointRounding.AwayFromZero);

        var byAuthor = completed
            .GroupBy(book => book.Author)
            .Select(group => new AuthorCountDto(group.Key, group.Count()))
            .OrderByDescending(author => author.Count)
            .ThenBy(author => author.Author, StringComparer.CurrentCulture)
            .ToList();

        return new ReadingStatisticsDto(
            query.Period, start, end, completed.Count, completedPages + readingPages, average, byAuthor);
    }

    private IQueryable<Book> OwnedBooks()
    {
        var userId = currentUser.RequireUserId();
        return db.Books.AsNoTracking().Where(book => book.UserId == userId);
    }
}
