using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class ListTableDefinitionsHandler(
    IReadRepository<TableDefinition> tableRepository)
    : IQueryHandler<ListTableDefinitionsQuery, IReadOnlyList<TableDefinition>>
{
    public async ValueTask<IReadOnlyList<TableDefinition>> Handle(
        ListTableDefinitionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var tables = await tableRepository.ListAsync(new AllTablesSpecification(), cancellationToken);
        return tables
            .OrderBy(table => table.EntitySetName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
