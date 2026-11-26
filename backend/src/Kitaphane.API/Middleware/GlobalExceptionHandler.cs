using System.Text.Json;
using FluentValidation;
using Kitaphane.Application.Common.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace Kitaphane.API.Middleware;

/// <summary>
/// Converts unhandled exceptions to <see cref="ErrorResponse"/>.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, response) = exception switch
        {
            AppException appException => (
                appException.StatusCode,
                new ErrorResponse(appException.Message, appException.Code)),

            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                CreateValidationResponse(validationException)),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ErrorResponse("An unexpected error occurred.", ErrorCodes.InternalError)),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static ErrorResponse CreateValidationResponse(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return ValidationErrorResponse(errors);
    }

    public static ErrorResponse ValidationErrorResponse(IDictionary<string, string[]> errors) =>
        new("One or more validation errors occurred.", ErrorCodes.ValidationError, errors);
}
