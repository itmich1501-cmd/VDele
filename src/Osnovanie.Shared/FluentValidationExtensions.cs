using FluentValidation;
using FluentValidation.Results;

namespace Osnovanie.Shared;

public static class FluentValidationExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        Error error)
    {
        return rule
            .WithErrorCode(error.Code)
            .WithMessage(error.Message);
    }
    
    public static Errors ToErrors(this ValidationResult validationResult) =>
        validationResult.Errors!.Select(e => Error.Validation(e.ErrorCode, e.ErrorMessage, e.PropertyName)).ToArray();
}