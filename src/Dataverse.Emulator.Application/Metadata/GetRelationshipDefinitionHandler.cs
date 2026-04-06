using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetRelationshipDefinitionHandler(
    IReadRepository<TableDefinition> tableRepository,
    LookupRelationshipDefinitionService lookupRelationshipDefinitionService)
    : IQueryHandler<GetRelationshipDefinitionQuery, ErrorOr<LookupRelationshipDefinition>>
{
    public async ValueTask<ErrorOr<LookupRelationshipDefinition>> Handle(
        GetRelationshipDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        var tables = await tableRepository.ListAsync(new AllTablesSpecification(), cancellationToken);
        return lookupRelationshipDefinitionService.Resolve(tables, query.SchemaName);
    }
}
