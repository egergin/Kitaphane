using FluentValidation;
using FluentValidation.Results;
using Kitaphane.Application.Common;
using Kitaphane.Application.Common.Errors;
using Kitaphane.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace Kitaphane.Application.Books;

/// <summary>
/// Book CRUD for the current user. Every query is scoped to the authenticated user's ID;
/// books of other users behave as if they do not exist (404).
/// </summary>
public sealed class BookService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<PagedResult<BookListItemDto>> GetBooksAsync(GetBooksQuery query, CancellationToken cancellationToken)
    {
        var books = OwnedBooks(query.Search);

        var totalCount = await books.CountAsync(cancellationToken);
        var items = await books
            .OrderByDescending(book => book.UpdatedAt)
            .ThenByDescending(book => book.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(book => new BookListItemDto(
                book.Id, book.Title, book.Author, book.CoverImage, book.CurrentPage, book.PageCount))
            .ToListAsync(cancellationToken);

        return new PagedResult<BookListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>"Okuduğum Kitaplar": completed books, most recently finished first.</summary>
    public async Task<PagedResult<CompletedBookDto>> GetCompletedBooksAsync(GetBooksQuery query, CancellationToken cancellationToken)
    {
        var books = OwnedBooks(query.Search).Where(book => book.Status == BookStatus.Completed);

        var totalCount = await books.CountAsync(cancellationToken);
        var items = await books
            .OrderByDescending(book => book.FinishDate)
            .ThenByDescending(book => book.UpdatedAt)
            .ThenByDescending(book => book.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(book => new CompletedBookDto(
                book.Id, book.Title, book.Author, book.CoverImage, book.Isbn, book.PageCount, book.Rating, book.FinishDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<CompletedBookDto>(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Changes only the status; the status/date rules apply (e.g. leaving COMPLETED clears the finish date).</summary>
    public async Task<BookDetailDto> UpdateStatusAsync(Guid id, UpdateBookStatusRequest request, CancellationToken cancellationToken)
    {
        var book = await FindOwnedBookAsync(id, cancellationToken);

        book.SetStatus(request.Status, Today(), isNew: false);
        await db.SaveChangesAsync(cancellationToken);

        return ToDetail(book);
    }

    public async Task<BookDetailDto> GetBookAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await FindOwnedBookAsync(id, cancellationToken);
        return ToDetail(book);
    }

    public async Task<BookDetailDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var book = new Book { Id = Guid.CreateVersion7(), UserId = currentUser.RequireUserId() };

        await ApplyAsync(book, request, isNew: true, cancellationToken);

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        return ToDetail(book);
    }

    public async Task<BookDetailDto> UpdateBookAsync(Guid id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var book = await FindOwnedBookAsync(id, cancellationToken);

        await ApplyAsync(book, request, isNew: false, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDetail(book);
    }

    public async Task DeleteBookAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await FindOwnedBookAsync(id, cancellationToken);

        db.Books.Remove(book);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAsync(Book book, IBookRequest request, bool isNew, CancellationToken cancellationToken)
    {
        var isbn = Isbn.Normalize(request.Isbn);

        if (isbn is not null && await db.Books.AnyAsync(
                other => other.UserId == book.UserId && other.Isbn == isbn && other.Id != book.Id, cancellationToken))
        {
            throw new ConflictException(ErrorCodes.BookIsbnExists, "A book with this ISBN is already on your shelf.");
        }

        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.PageCount = request.PageCount;
        book.CurrentPage = request.CurrentPage;
        book.CoverImage = string.IsNullOrWhiteSpace(request.CoverImage) ? null : request.CoverImage.Trim();
        book.Isbn = isbn;
        book.Rating = request.Rating;
        book.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        book.StartDate = request.StartDate;
        book.FinishDate = request.FinishDate;

        // Clients normally send the dates already (in the user's local date); the server only fills gaps.
        book.SetStatus(request.Status, Today(), isNew);

        if (book.HasInvalidDateRange)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(IBookRequest.FinishDate), "Finish date cannot be earlier than the start date."),
            ]);
        }
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    /// <summary>The current user's books, optionally filtered by title/author text.</summary>
    private IQueryable<Book> OwnedBooks(string? searchText)
    {
        var userId = currentUser.RequireUserId();
        var books = db.Books.AsNoTracking().Where(book => book.UserId == userId);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return books;
        }

        // Case-insensitive on both sides; Turkish dotless/dotted i (ı, I, İ) all match "i".
        // ToLowerInvariant leaves "İ" unchanged, so it is mapped explicitly (PostgreSQL lower() maps it to "i").
        var search = searchText.Trim().Replace('İ', 'i').ToLowerInvariant().Replace('ı', 'i');
        return books.Where(book =>
            book.Title.ToLower().Replace("ı", "i").Contains(search) ||
            book.Author.ToLower().Replace("ı", "i").Contains(search));
    }

    private async Task<Book> FindOwnedBookAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        return await db.Books.SingleOrDefaultAsync(book => book.Id == id && book.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.BookNotFound, "Book not found.");
    }

    private static BookDetailDto ToDetail(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn,
        book.PageCount,
        book.CurrentPage,
        book.Status,
        book.CoverImage,
        book.Rating,
        book.Notes,
        book.StartDate,
        book.FinishDate,
        book.CreatedAt,
        book.UpdatedAt);
}
