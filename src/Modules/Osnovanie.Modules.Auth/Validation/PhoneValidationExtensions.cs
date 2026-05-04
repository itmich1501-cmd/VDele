using FluentValidation;

namespace Osnovanie.Modules.Auth.Validation;

public static class PhoneValidationExtensions
{
    public static IRuleBuilderOptions<T, string> Phone<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Matches(@"^\+7\d{10}$")
            .WithMessage("Phone must be in format +7XXXXXXXXXX");
    }
}