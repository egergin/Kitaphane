using Kitaphane.Application.Common.Errors;

namespace Kitaphane.Infrastructure.Email;

internal static class EmailDeliveryErrors
{
    public static ServiceUnavailableException Failed() =>
        new(ErrorCodes.EmailDeliveryFailed, "The email could not be sent. Please try again later.");
}
