using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class DisassociateRowsCommandValidator : AbstractValidator<DisassociateRowsCommand>
{
    public DisassociateRowsCommandValidator()
    {
        RuleFor(command => command.PrimaryTableLogicalName).NotEmpty();
        RuleFor(command => command.PrimaryId).NotEqual(Guid.Empty);
        RuleFor(command => command.RelationshipSchemaName).NotEmpty();
        RuleFor(command => command.RelatedEntities).NotNull();
        RuleForEach(command => command.RelatedEntities).SetValidator(new RelatedRowReferenceValidator());
    }
}
