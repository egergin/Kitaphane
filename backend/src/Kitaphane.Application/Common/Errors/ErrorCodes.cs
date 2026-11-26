namespace Kitaphane.Application.Common.Errors;

/// <summary>
/// Machine-readable error codes returned in the "code" field of every error response.
/// The frontend maps these to user-facing messages.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";

    // Authentication
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string VerificationCodeInvalid = "VERIFICATION_CODE_INVALID";
    public const string VerificationCodeExpired = "VERIFICATION_CODE_EXPIRED";
    public const string VerificationAttemptsExceeded = "VERIFICATION_ATTEMPTS_EXCEEDED";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string InvalidResetToken = "INVALID_RESET_TOKEN";
    public const string InvalidCurrentPassword = "INVALID_CURRENT_PASSWORD";
    public const string PasswordSameAsCurrent = "PASSWORD_SAME_AS_CURRENT";

    // Books
    public const string BookNotFound = "BOOK_NOT_FOUND";
    public const string BookIsbnExists = "BOOK_ISBN_EXISTS";

    // Reading goals
    public const string ReadingGoalNotFound = "READING_GOAL_NOT_FOUND";

    // Google Books
    public const string GoogleBooksUnavailable = "GOOGLE_BOOKS_UNAVAILABLE";
}
