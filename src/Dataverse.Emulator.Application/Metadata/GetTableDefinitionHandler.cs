using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using ErrorOr;
using FluentValidation;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetTableDefinitionHandler(
    IReadRepository<TableDefinition> tableRepository,
    IValidator<GetTableDefinitionQuery> validator)
    : IQueryHandler<GetTableDefinitionQuery, ErrorOr<TableDefinition>>
{
    public async ValueTask<ErrorOr<TableDefinition>> Handle(
        GetTableDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var table = await tableRepository.SingleOrDefaultAsync(
            new TableByLogicalNameSpecification(query.LogicalName),
            cancellationToken);

        return table is not null
            ? table
            : DomainErrors.UnknownTable(query.LogicalName);
    }
}
