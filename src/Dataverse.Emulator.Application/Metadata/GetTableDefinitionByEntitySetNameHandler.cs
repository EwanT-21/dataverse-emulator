using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetTableDefinitionByEntitySetNameHandler(
    IReadRepository<TableDefinition> tableRepository)
    : IQueryHandler<GetTableDefinitionByEntitySetNameQuery, ErrorOr<TableDefinition>>
{
    public async ValueTask<ErrorOr<TableDefinition>> Handle(
        GetTableDefinitionByEntitySetNameQuery query,
        CancellationToken cancellationToken = default)
    {
        var table = await tableRepository.SingleOrDefaultAsync(
            new TableByEntitySetNameSpecification(query.EntitySetName),
            cancellationToken);

        return table is not null
            ? table
            : DomainErrors.UnknownTable(query.EntitySetName);
    }
}
