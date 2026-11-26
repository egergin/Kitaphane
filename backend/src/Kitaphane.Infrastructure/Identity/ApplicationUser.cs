using Kitaphane.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Kitaphane.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>, IHasTimestamps
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Profile image URL. File upload is not part of the MVP.</summary>
    public string? ProfileImage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
