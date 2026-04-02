using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class CreateRowCommandValidator : AbstractValidator<CreateRowCommand>
{
    public CreateRowCommandValidator()
    {
        RuleFor(command => command.TableLogicalName).NotEmpty();
        RuleFor(command => command.Values).NotNull();
    }
}
