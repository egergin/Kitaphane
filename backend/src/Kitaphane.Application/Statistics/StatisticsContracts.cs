namespace Kitaphane.Application.Statistics;

public sealed record StatisticsOverviewDto(int TotalBooks, int CompletedBooks, int ReadingBooks, int ToReadBooks);

/// <summary><c>period</c> is one of <c>1m</c>, <c>6m</c>, <c>1y</c>: a window ending today.</summary>
public sealed record ReadingStatisticsQuery(string Period);

public sealed record AuthorCountDto(string Author, int Count);

public sealed record ReadingStatisticsDto(
    string Period,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int CompletedBooks,
    int PagesRead,
    int AverageBookLength,
    IReadOnlyList<AuthorCountDto> BooksByAuthor);
