using Kitaphane.Domain.ReadingGoals;
using Kitaphane.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitaphane.Infrastructure.Persistence.Configurations;

internal sealed class ReadingGoalConfiguration : IEntityTypeConfiguration<ReadingGoal>
{
    public void Configure(EntityTypeBuilder<ReadingGoal> builder)
    {
        builder.ToTable("ReadingGoals", table =>
        {
            // Defense in depth: the same rules are validated by the API.
            table.HasCheckConstraint("CK_ReadingGoals_Target", "\"Target\" > 0");
            table.HasCheckConstraint("CK_ReadingGoals_Dates", "\"EndDate\" > \"StartDate\"");
        });

        builder.HasKey(goal => goal.Id);

        builder.Property(goal => goal.Type)
            .HasMaxLength(16)
            .HasConversion(
                type => ToDatabase(type),
                value => FromDatabase(value));

        builder.HasIndex(goal => goal.UserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(goal => goal.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static string ToDatabase(ReadingGoalType type) => type switch
    {
        ReadingGoalType.BookCount => "BOOK_COUNT",
        ReadingGoalType.PageCount => "PAGE_COUNT",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    private static ReadingGoalType FromDatabase(string value) => value switch
    {
        "BOOK_COUNT" => ReadingGoalType.BookCount,
        "PAGE_COUNT" => ReadingGoalType.PageCount,
        _ => throw new InvalidOperationException($"Unknown reading goal type '{value}'."),
    };
}
