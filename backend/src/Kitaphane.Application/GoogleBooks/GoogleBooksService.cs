using FluentValidation;
using FluentValidation.Results;
using Kitaphane.Domain.Books;

namespace Kitaphane.Application.GoogleBooks;

/// <summary>
/// Book lookup by title/author text or ISBN. Results are only suggestions; nothing is saved here.
/// </summary>
public sealed class GoogleBooksService(IGoogleBooksClient client)
{
    public Task<IReadOnlyList<GoogleBookSearchResultDto>> SearchAsync(GoogleBooksSearchQuery query, CancellationToken cancellationToken) =>
        client.SearchAsync(query.Query.Trim(), cancellationToken);

    public Task<IReadOnlyList<GoogleBookSearchResultDto>> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken)
    {
        var normalized = Isbn.Normalize(isbn);

        if (!Isbn.IsValid(normalized))
        {
            throw new ValidationException([new ValidationFailure("isbn", "ISBN must be a valid ISBN-10 or ISBN-13.")]);
        }

        return client.SearchAsync($"isbn:{normalized}", cancellationToken);
    }
}

public sealed class GoogleBooksSearchQueryValidator : AbstractValidator<GoogleBooksSearchQuery>
{
    public GoogleBooksSearchQueryValidator() =>
        RuleFor(x => x.Query).NotEmpty().MaximumLength(GoogleBooksSearchQuery.MaxQueryLength);
}
