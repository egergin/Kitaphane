using FluentValidation;

namespace Kitaphane.Application.ReadingGoals;

public static class ReadingGoalRules
{
    public const int MaxTarget = 1_000_000;
}

public sealed class SaveReadingGoalRequestValidator : AbstractValidator<SaveReadingGoalRequest>
{
    public SaveReadingGoalRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Target).GreaterThan(0).LessThanOrEqualTo(ReadingGoalRules.MaxTarget);
        RuleFor(x => x.StartDate).NotNull();
        RuleFor(x => x.EndDate).NotNull();
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate!.Value)
            .WithMessage("End date must be after the start date.")
            .When(x => x.StartDate is not null && x.EndDate is not null);
    }
}
