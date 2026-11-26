using Kitaphane.Application.ReadingGoals;
using Microsoft.AspNetCore.Mvc;

namespace Kitaphane.API.Controllers;

/// <summary>The authenticated user's reading goals with computed progress.</summary>
[ApiController]
[Route("api/reading-goals")]
public sealed class ReadingGoalsController(ReadingGoalService readingGoalService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReadingGoalDto>>> GetGoals(CancellationToken cancellationToken) =>
        Ok(await readingGoalService.GetGoalsAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReadingGoalDto>> CreateGoal(SaveReadingGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await readingGoalService.CreateGoalAsync(request, cancellationToken);
        return Created($"/api/reading-goals/{goal.Id}", goal);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReadingGoalDto>> UpdateGoal(
        Guid id, SaveReadingGoalRequest request, CancellationToken cancellationToken) =>
        Ok(await readingGoalService.UpdateGoalAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGoal(Guid id, CancellationToken cancellationToken)
    {
        await readingGoalService.DeleteGoalAsync(id, cancellationToken);
        return NoContent();
    }
}
