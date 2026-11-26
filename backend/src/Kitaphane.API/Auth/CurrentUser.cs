using Kitaphane.Application.Common;
using Kitaphane.Application.Common.Errors;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Kitaphane.API.Auth;

/// <summary>Reads the user ID from the validated access token ("sub" claim).</summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : null;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedException(ErrorCodes.Unauthorized, "Authentication is required.");
}
