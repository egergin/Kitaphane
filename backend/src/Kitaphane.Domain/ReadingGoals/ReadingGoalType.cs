namespace Kitaphane.Domain.ReadingGoals;

public enum ReadingGoalType
{
    /// <summary>Number of books completed in the goal's date range.</summary>
    BookCount,

    /// <summary>Pages read in the goal's date range (same formula as the reading statistics).</summary>
    PageCount,
}
