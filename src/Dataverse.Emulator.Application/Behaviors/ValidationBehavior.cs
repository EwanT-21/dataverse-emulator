using Dataverse.Emulator.Application.Common;
using FluentValidation;
using Mediator;

namespace Dataverse.Emulator.Application.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    private readonly IValidator<TMessage>[] validators = validators.ToArray();

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Length == 0)
        {
            throw new InvalidOperationException(
                $"No validator is registered for mediator message '{typeof(TMessage).FullName}'.");
        }

        var context = new ValidationContext<TMessage>(message);
        var validationResults = await Task.WhenAll(
            validators
                .Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (failures.Length > 0)
        {
            return ValidationResponseFactory.FromFailures<TResponse>(failures);
        }

        return await next(message, cancellationToken);
    }
}
