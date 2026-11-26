using Kitaphane.Application.Auth;

namespace Kitaphane.Application.Profile;

/// <summary>Name and profile image URL of the current user. Email cannot be changed in the MVP.</summary>
public sealed record UpdateProfileRequest(string Name, string? ProfileImage);

public interface IProfileService
{
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
}
