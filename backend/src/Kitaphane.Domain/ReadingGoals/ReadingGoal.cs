using Kitaphane.Domain.Common;

namespace Kitaphane.Domain.ReadingGoals;

/// <summary>
/// A user's reading goal for a freely chosen date range (e.g. 30 books in 2026). Progress is not stored;
/// it is computed from the user's books. Goals may overlap.
/// </summary>
public class ReadingGoal : IHasTimestamps
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ReadingGoalType Type { get; set; }

    /// <summary>Positive number of books or pages.</summary>
    public int Target { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Inclusive; always after <see cref="StartDate"/>.</summary>
    public DateOnly EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
