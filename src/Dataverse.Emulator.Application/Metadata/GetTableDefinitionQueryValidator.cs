using FluentValidation;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetTableDefinitionQueryValidator : AbstractValidator<GetTableDefinitionQuery>
{
    public GetTableDefinitionQueryValidator()
    {
        RuleFor(query => query.LogicalName).NotEmpty();
    }
}
