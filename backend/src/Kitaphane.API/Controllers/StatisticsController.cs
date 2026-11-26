using Kitaphane.Application.Statistics;
using Microsoft.AspNetCore.Mvc;

namespace Kitaphane.API.Controllers;

/// <summary>Book and reading statistics of the authenticated user, computed from their books.</summary>
[ApiController]
[Route("api/statistics")]
public sealed class StatisticsController(StatisticsService statisticsService) : ControllerBase
{
    /// <summary>Book counts by status.</summary>
    [HttpGet("overview")]
    public async Task<ActionResult<StatisticsOverviewDto>> GetOverview(CancellationToken cancellationToken) =>
        Ok(await statisticsService.GetOverviewAsync(cancellationToken));

    /// <summary>Reading statistics for the last month (1m), 6 months (6m) or year (1y).</summary>
    [HttpGet("reading")]
    public async Task<ActionResult<ReadingStatisticsDto>> GetReading(
        [FromQuery] ReadingStatisticsQuery request, CancellationToken cancellationToken) =>
        Ok(await statisticsService.GetReadingAsync(request, cancellationToken));
}
