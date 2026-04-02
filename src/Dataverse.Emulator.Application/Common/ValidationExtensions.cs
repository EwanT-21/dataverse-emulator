using ErrorOr;
using FluentValidation.Results;

namespace Dataverse.Emulator.Application.Common;

internal static class ValidationExtensions
{
    public static List<Error> ToErrors(this ValidationResult validationResult)
    {
        return validationResult.Errors
            .Select(failure => Error.Validation(
                code: $"Validation.{failure.PropertyName}",
                description: failure.ErrorMessage))
            .ToList();
    }
}
