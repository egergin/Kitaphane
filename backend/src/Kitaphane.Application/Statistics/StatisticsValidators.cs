using FluentValidation;

namespace Kitaphane.Application.Statistics;

public sealed class ReadingStatisticsQueryValidator : AbstractValidator<ReadingStatisticsQuery>
{
    public ReadingStatisticsQueryValidator() =>
        RuleFor(x => x.Period)
            .Must(period => StatisticsService.Periods.ContainsKey(period ?? string.Empty))
            .WithMessage("Period must be one of: 1m, 6m, 1y.");
}
