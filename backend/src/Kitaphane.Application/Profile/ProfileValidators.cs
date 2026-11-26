using FluentValidation;
using Kitaphane.Application.Auth;
using Kitaphane.Application.Common;

namespace Kitaphane.Application.Profile;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name is required.")
            .MaximumLength(AuthRules.NameMaxLength);

        RuleFor(x => x.ProfileImage)
            .MaximumLength(UrlRules.ImageUrlMaxLength)
            .Must(UrlRules.IsHttpUrl).WithMessage("Profile image must be a valid http(s) URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImage));
    }
}
