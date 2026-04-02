using FluentValidation;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetTableDefinitionByEntitySetNameQueryValidator : AbstractValidator<GetTableDefinitionByEntitySetNameQuery>
{
    public GetTableDefinitionByEntitySetNameQueryValidator()
    {
        RuleFor(query => query.EntitySetName).NotEmpty();
    }
}
