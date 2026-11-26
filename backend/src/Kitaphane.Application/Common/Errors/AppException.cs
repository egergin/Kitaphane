namespace Kitaphane.Application.Common.Errors;

/// <summary>
/// Base type for expected application errors. Translated to the API error format by the API layer.
/// </summary>
public abstract class AppException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;

    public int StatusCode { get; } = statusCode;
}

public sealed class BadRequestException(string code, string message) : AppException(code, message, 400);

public sealed class UnauthorizedException(string code, string message) : AppException(code, message, 401);

public sealed class NotFoundException(string code, string message) : AppException(code, message, 404);

public sealed class ConflictException(string code, string message) : AppException(code, message, 409);

public sealed class BadGatewayException(string code, string message) : AppException(code, message, 502);
