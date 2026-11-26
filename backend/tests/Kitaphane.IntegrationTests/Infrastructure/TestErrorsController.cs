using FluentValidation;
using FluentValidation.Results;
using Kitaphane.Application.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitaphane.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only endpoints that throw each error type so the error format can be verified end to end.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/test-errors")]
public sealed class TestErrorsController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() => throw new NotFoundException("BOOK_NOT_FOUND", "Book not found.");

    [HttpGet("conflict")]
    public IActionResult ThrowConflict() => throw new ConflictException("TAG_EXISTS", "Tag already exists.");

    [HttpGet("validation")]
    public IActionResult ThrowValidation() => throw new ValidationException(
    [
        new ValidationFailure("PageCount", "PageCount must be greater than 0."),
    ]);

    [HttpGet("unexpected")]
    public IActionResult ThrowUnexpected() => throw new InvalidOperationException("secret internal detail");

    [HttpPost("model")]
    public IActionResult BindModel(TestModel model) => Ok(model);

    public sealed record TestModel(int PageCount);
}
