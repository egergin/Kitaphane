using Kitaphane.Application.Books;
using Kitaphane.Domain.Books;

namespace Kitaphane.UnitTests;

public sealed class BookValidatorTests
{
    private readonly CreateBookRequestValidator _validator = new();
    private readonly GetBooksQueryValidator _queryValidator = new();

    private static CreateBookRequest Valid() => new("Suç ve Ceza", "Fyodor Dostoyevski", 687);

    [Fact]
    public void Minimal_book_is_valid() => Assert.True(_validator.Validate(Valid()).IsValid);

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void Invalid_books_are_rejected(CreateBookRequest request, string property)
    {
        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == property);
    }

    public static TheoryData<CreateBookRequest, string> InvalidRequests() => new()
    {
        { Valid() with { Title = "" }, nameof(CreateBookRequest.Title) },
        { Valid() with { Author = " " }, nameof(CreateBookRequest.Author) },
        { Valid() with { PageCount = 0 }, nameof(CreateBookRequest.PageCount) },
        { Valid() with { CurrentPage = -1 }, nameof(CreateBookRequest.CurrentPage) },
        { Valid() with { CurrentPage = 700 }, nameof(CreateBookRequest.CurrentPage) },
        { Valid() with { Rating = 0 }, nameof(CreateBookRequest.Rating) },
        { Valid() with { Rating = 11 }, nameof(CreateBookRequest.Rating) },
        { Valid() with { Isbn = "123" }, nameof(CreateBookRequest.Isbn) },
        { Valid() with { CoverImage = "javascript:alert(1)" }, nameof(CreateBookRequest.CoverImage) },
        { Valid() with { Status = (BookStatus)42 }, nameof(CreateBookRequest.Status) },
        {
            Valid() with { StartDate = new DateOnly(2026, 5, 2), FinishDate = new DateOnly(2026, 5, 1) },
            nameof(CreateBookRequest.FinishDate)
        },
    };

    [Theory]
    [InlineData(18, true)]
    [InlineData(36, true)]
    [InlineData(72, true)]
    [InlineData(108, true)]
    [InlineData(20, false)]
    [InlineData(0, false)]
    public void Page_size_must_be_one_of_the_allowed_values(int pageSize, bool isValid) =>
        Assert.Equal(isValid, _queryValidator.Validate(new GetBooksQuery { PageSize = pageSize }).IsValid);
}
