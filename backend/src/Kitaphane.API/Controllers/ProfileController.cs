using Kitaphane.Application.Auth;
using Kitaphane.Application.Common;
using Kitaphane.Application.Profile;
using Microsoft.AspNetCore.Mvc;

namespace Kitaphane.API.Controllers;

/// <summary>The authenticated user's profile. Password changes go through <c>/api/auth/change-password</c>.</summary>
[ApiController]
[Route("api/profile")]
public sealed class ProfileController(IProfileService profileService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken cancellationToken) =>
        Ok(await profileService.GetProfileAsync(currentUser.RequireUserId(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profileService.UpdateProfileAsync(currentUser.RequireUserId(), request, cancellationToken));
}
