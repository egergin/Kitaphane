using Kitaphane.Application.Books;
using Kitaphane.Domain.Books;
using Kitaphane.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitaphane.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books", table =>
        {
            // Defense in depth: the same rules are validated by the API.
            table.HasCheckConstraint("CK_Books_PageCount", "\"PageCount\" > 0");
            table.HasCheckConstraint("CK_Books_CurrentPage", "\"CurrentPage\" >= 0 AND \"CurrentPage\" <= \"PageCount\"");
            table.HasCheckConstraint("CK_Books_Rating", "\"Rating\" IS NULL OR (\"Rating\" BETWEEN 1 AND 10)");
        });

        builder.HasKey(book => book.Id);

        builder.Property(book => book.Title).IsRequired().HasMaxLength(BookRules.TitleMaxLength);
        builder.Property(book => book.Author).IsRequired().HasMaxLength(BookRules.AuthorMaxLength);
        builder.Property(book => book.CoverImage).HasMaxLength(BookRules.CoverImageMaxLength);
        builder.Property(book => book.Isbn).HasMaxLength(13);
        builder.Property(book => book.Notes).HasMaxLength(BookRules.NotesMaxLength);

        builder.Property(book => book.Status)
            .HasMaxLength(16)
            .HasConversion(
                status => ToDatabase(status),
                value => FromDatabase(value));

        // Home page: the user's books, most recently updated first.
        builder.HasIndex(book => new { book.UserId, book.UpdatedAt }).IsDescending(false, true);

        // ISBN is unique per user; books without an ISBN are not restricted.
        builder.HasIndex(book => new { book.UserId, book.Isbn })
            .IsUnique()
            .HasFilter("\"Isbn\" IS NOT NULL");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(book => book.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static string ToDatabase(BookStatus status) => status switch
    {
        BookStatus.ToRead => "TO_READ",
        BookStatus.Reading => "READING",
        BookStatus.Completed => "COMPLETED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static BookStatus FromDatabase(string value) => value switch
    {
        "TO_READ" => BookStatus.ToRead,
        "READING" => BookStatus.Reading,
        "COMPLETED" => BookStatus.Completed,
        _ => throw new InvalidOperationException($"Unknown book status '{value}'."),
    };
}
