using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class AssociateRowsCommandValidator : AbstractValidator<AssociateRowsCommand>
{
    public AssociateRowsCommandValidator()
    {
        RuleFor(command => command.PrimaryTableLogicalName).NotEmpty();
        RuleFor(command => command.PrimaryId).NotEqual(Guid.Empty);
        RuleFor(command => command.RelationshipSchemaName).NotEmpty();
        RuleFor(command => command.RelatedEntities).NotNull();
        RuleForEach(command => command.RelatedEntities).SetValidator(new RelatedRowReferenceValidator());
    }
}

public sealed class RelatedRowReferenceValidator : AbstractValidator<RelatedRowReference>
{
    public RelatedRowReferenceValidator()
    {
        RuleFor(reference => reference.TableLogicalName).NotEmpty();
        RuleFor(reference => reference.Id).NotEqual(Guid.Empty);
    }
}
