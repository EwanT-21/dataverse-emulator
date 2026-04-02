using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class DeleteRowCommandValidator : AbstractValidator<DeleteRowCommand>
{
    public DeleteRowCommandValidator()
    {
        RuleFor(command => command.TableLogicalName).NotEmpty();
        RuleFor(command => command.Id).NotEqual(Guid.Empty);
    }
}
