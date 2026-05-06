using FluentValidation;
using Osnovanie.Shared;

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

public static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> Password<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithError(Error.Validation(
                "auth.password.empty",
                "Пароль обязателен",
                "password"))
            .MinimumLength(6)
            .WithError(Error.Validation(
                "auth.password.too_short",
                "Минимум 6 символов",
                "password"))
            .MaximumLength(100)
            .WithError(Error.Validation(
                "auth.password.too_long",
                "Максимум 100 символов",
                "password"));
    }
}

public static class UserNameValidationExtensions
{
    public static IRuleBuilderOptions<T, string> FirstName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithError(Error.Validation(
                "auth.firstname.empty",
                "Имя обязательно",
                "firstName"))
            .MaximumLength(50)
            .WithError(Error.Validation(
                "auth.firstname.too_long",
                "Максимум 50 символов",
                "firstName"));
    }
}