using Kitaphane.Domain.Common;

namespace Kitaphane.Domain.Books;

/// <summary>
/// A book on a user's personal shelf. Every user has their own copy of a book (no shared catalog).
/// </summary>
public class Book : IHasTimestamps
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public int PageCount { get; set; }

    public int CurrentPage { get; set; }

    public BookStatus Status { get; private set; } = BookStatus.ToRead;

    public string? CoverImage { get; set; }

    /// <summary>Normalized ISBN-10 or ISBN-13 (digits, optional trailing X), unique per user.</summary>
    public string? Isbn { get; set; }

    /// <summary>1–10, or null when not rated.</summary>
    public int? Rating { get; set; }

    public string? Notes { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? FinishDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Sets the status and applies the status/date rules (docs/requirements/SRS.md FR-02):
    /// <list type="bullet">
    /// <item>Moving to READING fills an empty StartDate with today.</item>
    /// <item>A COMPLETED book always has CurrentPage = PageCount and a FinishDate (today when empty).</item>
    /// <item>A book that is not COMPLETED has no FinishDate.</item>
    /// <item>Reaching the last page does not change the status by itself.</item>
    /// </list>
    /// Call after setting PageCount, CurrentPage and the dates from the request.
    /// </summary>
    public void SetStatus(BookStatus status, DateOnly today, bool isNew)
    {
        var enteringReading = status == BookStatus.Reading && (isNew || Status != BookStatus.Reading);

        Status = status;

        if (enteringReading)
        {
            StartDate ??= today;
        }

        if (status == BookStatus.Completed)
        {
            CurrentPage = PageCount;
            FinishDate ??= today;
        }
        else
        {
            FinishDate = null;
        }
    }

    /// <summary>True when both dates are set and the book was finished before it was started.</summary>
    public bool HasInvalidDateRange => StartDate is { } start && FinishDate is { } finish && start > finish;
}
