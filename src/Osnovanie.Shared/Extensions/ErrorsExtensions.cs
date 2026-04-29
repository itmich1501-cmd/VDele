namespace Osnovanie.Shared.Extensions;

public static class ErrorsExtensions
{
    public static Errors ToErrors(this IEnumerable<Error> errors)
        => new(errors);
}