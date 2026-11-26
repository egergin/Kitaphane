using Kitaphane.Application.Common.Errors;

namespace Kitaphane.UnitTests;

public sealed class AppExceptionTests
{
    [Fact]
    public void Exceptions_carry_code_and_status()
    {
        AppException[] exceptions =
        [
            new BadRequestException("X", "m"),
            new UnauthorizedException("X", "m"),
            new NotFoundException("X", "m"),
            new ConflictException("X", "m"),
        ];

        Assert.Equal([400, 401, 404, 409], exceptions.Select(exception => exception.StatusCode));
        Assert.All(exceptions, exception => Assert.Equal("X", exception.Code));
    }
}
