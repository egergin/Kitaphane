using Kitaphane.Domain.Books;

namespace Kitaphane.UnitTests;

public sealed class IsbnTests
{
    [Theory]
    [InlineData("978-0-321-82248-2", "9780321822482")]
    [InlineData("0 306 40615 2", "0306406152")]
    [InlineData("080442957x", "080442957X")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void Normalize_removes_separators(string? input, string? expected) =>
        Assert.Equal(expected, Isbn.Normalize(input));

    [Theory]
    [InlineData("9780321822482", true)]   // ISBN-13
    [InlineData("9780306406157", true)]   // ISBN-13
    [InlineData("0306406152", true)]      // ISBN-10
    [InlineData("080442957X", true)]      // ISBN-10 with X check digit
    [InlineData("9780321822483", false)]  // wrong check digit
    [InlineData("0306406153", false)]     // wrong check digit
    [InlineData("12345", false)]
    [InlineData("97803218224X2", false)]
    public void IsValid_checks_length_and_check_digit(string isbn, bool expected) =>
        Assert.Equal(expected, Isbn.IsValid(isbn));
}
