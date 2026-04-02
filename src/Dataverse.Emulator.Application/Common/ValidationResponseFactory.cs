using System.Reflection;
using ErrorOr;
using FluentValidation.Results;

namespace Dataverse.Emulator.Application.Common;

internal static class ValidationResponseFactory
{
    public static TResponse FromFailures<TResponse>(IEnumerable<ValidationFailure> failures)
        => FromErrors<TResponse>(failures.ToErrors());

    private static TResponse FromErrors<TResponse>(List<Error> errors)
    {
        var responseType = typeof(TResponse);
        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(ErrorOr<>))
        {
            throw new InvalidOperationException(
                $"Validation responses must be '{typeof(ErrorOr<>).FullName}' but got '{responseType.FullName}'.");
        }

        var fromMethod = responseType.GetMethod(
            nameof(ErrorOr<int>.From),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(List<Error>)]);

        if (fromMethod is null)
        {
            throw new InvalidOperationException(
                $"Unable to map validation errors to response type '{responseType.FullName}'.");
        }

        return (TResponse)fromMethod.Invoke(null, [errors])!;
    }
}
