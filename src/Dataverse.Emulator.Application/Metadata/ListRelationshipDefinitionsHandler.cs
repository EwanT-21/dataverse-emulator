using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class ListRelationshipDefinitionsHandler(
    IReadRepository<TableDefinition> tableRepository,
    LookupRelationshipDefinitionService lookupRelationshipDefinitionService)
    : IQueryHandler<ListRelationshipDefinitionsQuery, ErrorOr<LookupRelationshipDefinition[]>>
{
    public async ValueTask<ErrorOr<LookupRelationshipDefinition[]>> Handle(
        ListRelationshipDefinitionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var tables = await tableRepository.ListAsync(new AllTablesSpecification(), cancellationToken);
        return lookupRelationshipDefinitionService.List(tables);
    }
}
