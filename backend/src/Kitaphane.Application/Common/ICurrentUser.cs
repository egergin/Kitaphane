namespace Kitaphane.Application.Common;

/// <summary>
/// The authenticated user of the current request. The ID always comes from the validated access token,
/// never from request data.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>Returns the user ID or throws <see cref="Errors.UnauthorizedException"/>.</summary>
    Guid RequireUserId();
}
