using Kitaphane.Application.Common.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace Kitaphane.API.Middleware;

/// <summary>
/// Writes an <see cref="ErrorResponse"/> body for error status codes produced without a body
/// (unknown routes, unsupported methods, authentication challenges).
/// </summary>
public static class StatusCodeResponses
{
    public static Task WriteAsync(StatusCodeContext context)
    {
        var response = context.HttpContext.Response;

        var error = response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => new ErrorResponse("Authentication is required.", ErrorCodes.Unauthorized),
            StatusCodes.Status403Forbidden => new ErrorResponse("Access is denied.", ErrorCodes.Forbidden),
            StatusCodes.Status404NotFound => new ErrorResponse("Resource not found.", ErrorCodes.NotFound),
            StatusCodes.Status405MethodNotAllowed => new ErrorResponse("Method not allowed.", ErrorCodes.MethodNotAllowed),
            StatusCodes.Status429TooManyRequests => new ErrorResponse("Too many requests.", ErrorCodes.RateLimited),
            _ => null,
        };

        return error is null
            ? Task.CompletedTask
            : response.WriteAsJsonAsync(error, context.HttpContext.RequestAborted);
    }
}
