using Kitaphane.Application.Auth;
using Kitaphane.Application.Common.Errors;
using Kitaphane.Application.Profile;
using Microsoft.AspNetCore.Identity;

namespace Kitaphane.Infrastructure.Identity;

/// <summary>Reads and updates the current user's name and profile image URL.</summary>
public sealed class ProfileService(UserManager<ApplicationUser> userManager) : IProfileService
{
    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken) =>
        ToDto(await FindUserAsync(userId));

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId);

        user.Name = request.Name.Trim();
        user.ProfileImage = string.IsNullOrWhiteSpace(request.ProfileImage) ? null : request.ProfileImage.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Profile update failed: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }

        return ToDto(user);
    }

    private async Task<ApplicationUser> FindUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString())
        ?? throw new UnauthorizedException(ErrorCodes.Unauthorized, "Authentication is required.");

    private static UserDto ToDto(ApplicationUser user) => new(user.Id, user.Name, user.Email!, user.ProfileImage);
}
