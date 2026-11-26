using Kitaphane.Application.GoogleBooks;
using Microsoft.AspNetCore.Mvc;

namespace Kitaphane.API.Controllers;

/// <summary>Google Books lookup for the "add book" form. Results are suggestions and are never saved directly.</summary>
[ApiController]
[Route("api/google-books")]
public sealed class GoogleBooksController(GoogleBooksService googleBooksService) : ControllerBase
{
    /// <summary>Search by title and/or author text.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<GoogleBookSearchResultDto>>> Search(
        [FromQuery] GoogleBooksSearchQuery request, CancellationToken cancellationToken) =>
        Ok(await googleBooksService.SearchAsync(request, cancellationToken));

    /// <summary>Search by ISBN-10 or ISBN-13 (hyphens allowed).</summary>
    [HttpGet("isbn/{isbn}")]
    public async Task<ActionResult<IReadOnlyList<GoogleBookSearchResultDto>>> SearchByIsbn(
        string isbn, CancellationToken cancellationToken) =>
        Ok(await googleBooksService.SearchByIsbnAsync(isbn, cancellationToken));
}
