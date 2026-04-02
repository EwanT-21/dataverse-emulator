using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class UpdateRowCommandValidator : AbstractValidator<UpdateRowCommand>
{
    public UpdateRowCommandValidator()
    {
        RuleFor(command => command.TableLogicalName).NotEmpty();
        RuleFor(command => command.Id).NotEqual(Guid.Empty);
        RuleFor(command => command.Values).NotNull();
    }
}
