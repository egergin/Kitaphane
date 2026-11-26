using Kitaphane.Application.Books;
using Kitaphane.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kitaphane.API.Controllers;

/// <summary>The authenticated user's books. The user is always taken from the access token.</summary>
[ApiController]
[Route("api/books")]
public sealed class BooksController(BookService bookService) : ControllerBase
{
    /// <summary>Books of the current user, most recently updated first.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookListItemDto>>> GetBooks(
        [FromQuery] GetBooksQuery query, CancellationToken cancellationToken) =>
        Ok(await bookService.GetBooksAsync(query, cancellationToken));

    /// <summary>Completed books of the current user, most recently finished first ("Okuduğum Kitaplar").</summary>
    [HttpGet("completed")]
    public async Task<ActionResult<PagedResult<CompletedBookDto>>> GetCompletedBooks(
        [FromQuery] GetBooksQuery query, CancellationToken cancellationToken) =>
        Ok(await bookService.GetCompletedBooksAsync(query, cancellationToken));

    /// <summary>Changes only the reading status; status/date rules apply.</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<BookDetailDto>> UpdateStatus(
        Guid id, UpdateBookStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await bookService.UpdateStatusAsync(id, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookDetailDto>> GetBook(Guid id, CancellationToken cancellationToken) =>
        Ok(await bookService.GetBookAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<BookDetailDto>> CreateBook(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var book = await bookService.CreateBookAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookDetailDto>> UpdateBook(
        Guid id, UpdateBookRequest request, CancellationToken cancellationToken) =>
        Ok(await bookService.UpdateBookAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        await bookService.DeleteBookAsync(id, cancellationToken);
        return NoContent();
    }
}
