using ErrorOr;
using FluentValidation;
using FluentValidation.Results;

namespace Dataverse.Emulator.Application.Common;

internal static class ValidationExtensions
{
    public static List<Error> ToErrors(this ValidationResult validationResult)
        => validationResult.Errors.ToErrors();

    public static List<Error> ToErrors(this IEnumerable<ValidationFailure> failures)
    {
        return failures
            .Select(failure => Error.Validation(
                code: $"Validation.{failure.PropertyName}",
                description: failure.ErrorMessage))
            .ToList();
    }
}
