using Kitaphane.Domain.ReadingGoals;

namespace Kitaphane.Application.ReadingGoals;

/// <summary>Create and update use the same body. Dates are nullable only so a missing value is a validation error.</summary>
public sealed record SaveReadingGoalRequest(ReadingGoalType Type, int Target, DateOnly? StartDate, DateOnly? EndDate);

/// <summary>
/// <c>Current</c> is the real progress (may exceed the target); <c>Percentage</c> is Current / Target
/// rounded down and is not capped at 100.
/// </summary>
public sealed record ReadingGoalDto(
    Guid Id,
    ReadingGoalType Type,
    int Target,
    DateOnly StartDate,
    DateOnly EndDate,
    int Current,
    int Percentage);
