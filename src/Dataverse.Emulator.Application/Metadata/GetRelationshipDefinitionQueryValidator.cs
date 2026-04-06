using FluentValidation;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetRelationshipDefinitionQueryValidator : AbstractValidator<GetRelationshipDefinitionQuery>
{
    public GetRelationshipDefinitionQueryValidator()
    {
        RuleFor(query => query.SchemaName).NotEmpty();
    }
}
