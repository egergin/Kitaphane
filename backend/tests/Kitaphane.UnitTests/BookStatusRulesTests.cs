using Kitaphane.Domain.Books;

namespace Kitaphane.UnitTests;

public sealed class BookStatusRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    private static Book NewBook(int pageCount = 300, int currentPage = 0) =>
        new() { Title = "Kitap", Author = "Yazar", PageCount = pageCount, CurrentPage = currentPage };

    [Fact]
    public void Completing_a_book_sets_current_page_and_finish_date()
    {
        var book = NewBook(currentPage: 120);

        book.SetStatus(BookStatus.Completed, Today, isNew: false);

        Assert.Equal(300, book.CurrentPage);
        Assert.Equal(Today, book.FinishDate);
    }

    [Fact]
    public void Completing_keeps_an_explicit_finish_date()
    {
        var book = NewBook();
        book.FinishDate = new DateOnly(2026, 1, 10);

        book.SetStatus(BookStatus.Completed, Today, isNew: true);

        Assert.Equal(new DateOnly(2026, 1, 10), book.FinishDate);
    }

    [Fact]
    public void Starting_to_read_fills_empty_start_date()
    {
        var book = NewBook();

        book.SetStatus(BookStatus.Reading, Today, isNew: false);

        Assert.Equal(Today, book.StartDate);
    }

    [Fact]
    public void Starting_to_read_keeps_existing_start_date()
    {
        var book = NewBook();
        book.StartDate = new DateOnly(2026, 3, 1);

        book.SetStatus(BookStatus.Reading, Today, isNew: true);

        Assert.Equal(new DateOnly(2026, 3, 1), book.StartDate);
    }

    [Fact]
    public void Staying_in_reading_does_not_refill_a_cleared_start_date()
    {
        var book = NewBook();
        book.SetStatus(BookStatus.Reading, Today, isNew: true);

        book.StartDate = null;
        book.SetStatus(BookStatus.Reading, Today.AddDays(3), isNew: false);

        Assert.Null(book.StartDate);
    }

    [Theory]
    [InlineData(BookStatus.Reading)]
    [InlineData(BookStatus.ToRead)]
    public void Leaving_completed_clears_finish_date(BookStatus newStatus)
    {
        var book = NewBook();
        book.SetStatus(BookStatus.Completed, Today, isNew: true);

        book.SetStatus(newStatus, Today, isNew: false);

        Assert.Null(book.FinishDate);
        Assert.Equal(300, book.CurrentPage); // current page is kept
    }

    [Fact]
    public void Reaching_the_last_page_does_not_complete_the_book()
    {
        var book = NewBook(currentPage: 300);

        book.SetStatus(BookStatus.Reading, Today, isNew: true);

        Assert.Equal(BookStatus.Reading, book.Status);
        Assert.Null(book.FinishDate);
    }

    [Fact]
    public void Finish_before_start_is_an_invalid_range()
    {
        var book = NewBook();
        book.StartDate = Today;
        book.FinishDate = Today.AddDays(-1);

        Assert.True(book.HasInvalidDateRange);
    }
}
