using FluentValidation;
using Kitaphane.Application.Common;
using Kitaphane.Domain.Books;

namespace Kitaphane.Application.Books;

public static class BookRules
{
    public const int TitleMaxLength = 300;
    public const int AuthorMaxLength = 300;
    public const int NotesMaxLength = 5000;
    public const int CoverImageMaxLength = 2048;
    public const int MinRating = 1;
    public const int MaxRating = 10;
}

/// <summary>Validation shared by create and update (docs/api/api-design.md §11).</summary>
public abstract class BookRequestValidator<T> : AbstractValidator<T> where T : IBookRequest
{
    protected BookRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(BookRules.TitleMaxLength);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(BookRules.AuthorMaxLength);
        RuleFor(x => x.PageCount).GreaterThan(0);
        RuleFor(x => x.CurrentPage)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(x => x.PageCount).WithMessage("Current page cannot be greater than the page count.");
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Rating).InclusiveBetween(BookRules.MinRating, BookRules.MaxRating).When(x => x.Rating is not null);
        RuleFor(x => x.Notes).MaximumLength(BookRules.NotesMaxLength);

        RuleFor(x => x.CoverImage)
            .MaximumLength(BookRules.CoverImageMaxLength)
            .Must(UrlRules.IsHttpUrl).WithMessage("Cover image must be a valid http(s) URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImage));

        RuleFor(x => x.Isbn)
            .Must(isbn => Isbn.IsValid(Isbn.Normalize(isbn)))
            .WithMessage("ISBN must be a valid ISBN-10 or ISBN-13.")
            .When(x => !string.IsNullOrWhiteSpace(x.Isbn));

        RuleFor(x => x.FinishDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("Finish date cannot be earlier than the start date.")
            .When(x => x.StartDate is not null && x.FinishDate is not null);
    }
}

public sealed class CreateBookRequestValidator : BookRequestValidator<CreateBookRequest>;

public sealed class UpdateBookRequestValidator : BookRequestValidator<UpdateBookRequest>;

public sealed class GetBooksQueryValidator : AbstractValidator<GetBooksQuery>
{
    public GetBooksQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize)
            .Must(size => GetBooksQuery.AllowedPageSizes.Contains(size))
            .WithMessage($"Page size must be one of: {string.Join(", ", GetBooksQuery.AllowedPageSizes)}.");
        RuleFor(x => x.Search).MaximumLength(GetBooksQuery.MaxSearchLength);
    }
}

public sealed class UpdateBookStatusRequestValidator : AbstractValidator<UpdateBookStatusRequest>
{
    public UpdateBookStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}
