using Kitaphane.Application.Common;
using Kitaphane.Application.Common.Errors;
using Kitaphane.Application.Statistics;
using Kitaphane.Domain.ReadingGoals;
using Microsoft.EntityFrameworkCore;

namespace Kitaphane.Application.ReadingGoals;

/// <summary>
/// Reading goals of the current user. Progress is computed from the user's books with the reading
/// statistics formulas over the goal's [StartDate, EndDate] range. Other users' goals behave as missing (404).
/// </summary>
public sealed class ReadingGoalService(IAppDbContext db, ICurrentUser currentUser)
{
    /// <summary>Goals ordered by start date (latest first).</summary>
    public async Task<IReadOnlyList<ReadingGoalDto>> GetGoalsAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var goals = await db.ReadingGoals
            .AsNoTracking()
            .Where(goal => goal.UserId == userId)
            .OrderByDescending(goal => goal.StartDate)
            .ThenByDescending(goal => goal.EndDate)
            .ThenByDescending(goal => goal.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<ReadingGoalDto>(goals.Count);
        foreach (var goal in goals)
        {
            result.Add(await ToDtoAsync(goal, cancellationToken));
        }

        return result;
    }

    public async Task<ReadingGoalDto> CreateGoalAsync(SaveReadingGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = new ReadingGoal { Id = Guid.CreateVersion7(), UserId = currentUser.RequireUserId() };
        Apply(goal, request);

        db.ReadingGoals.Add(goal);
        await db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(goal, cancellationToken);
    }

    public async Task<ReadingGoalDto> UpdateGoalAsync(Guid id, SaveReadingGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await FindOwnedGoalAsync(id, cancellationToken);
        Apply(goal, request);
        await db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(goal, cancellationToken);
    }

    public async Task DeleteGoalAsync(Guid id, CancellationToken cancellationToken)
    {
        var goal = await FindOwnedGoalAsync(id, cancellationToken);
        db.ReadingGoals.Remove(goal);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(ReadingGoal goal, SaveReadingGoalRequest request)
    {
        goal.Type = request.Type;
        goal.Target = request.Target;
        goal.StartDate = request.StartDate!.Value;
        goal.EndDate = request.EndDate!.Value;
    }

    private async Task<ReadingGoal> FindOwnedGoalAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        return await db.ReadingGoals.SingleOrDefaultAsync(goal => goal.Id == id && goal.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.ReadingGoalNotFound, "Reading goal not found.");
    }

    private async Task<ReadingGoalDto> ToDtoAsync(ReadingGoal goal, CancellationToken cancellationToken)
    {
        var books = db.Books.AsNoTracking().Where(book => book.UserId == goal.UserId);

        var current = goal.Type == ReadingGoalType.BookCount
            ? await books.CompletedIn(goal.StartDate, goal.EndDate).CountAsync(cancellationToken)
            : await books.PagesReadInAsync(goal.StartDate, goal.EndDate, cancellationToken);

        var percentage = (int)Math.Floor(current * 100.0 / goal.Target);

        return new ReadingGoalDto(goal.Id, goal.Type, goal.Target, goal.StartDate, goal.EndDate, current, percentage);
    }
}
