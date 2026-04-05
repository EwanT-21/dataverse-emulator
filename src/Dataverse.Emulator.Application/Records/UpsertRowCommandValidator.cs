using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class UpsertRowCommandValidator : AbstractValidator<UpsertRowCommand>
{
    public UpsertRowCommandValidator()
    {
        RuleFor(command => command.TableLogicalName).NotEmpty();
        RuleFor(command => command.Values).NotNull();
        RuleFor(command => command.Id)
            .NotEqual(Guid.Empty)
            .When(command => command.Id.HasValue);
    }
}
