namespace Kitaphane.Domain.Common;

/// <summary>
/// Entities with creation/update timestamps. Values are set automatically (UTC) when changes are saved.
/// </summary>
public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
}
