using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class ListRowsHandler(
    IReadRepository<TableDefinition> tableRepository,
    IRecordQueryService recordQueryService,
    QueryValidationService queryValidationService)
    : IQueryHandler<ListRowsQuery, ErrorOr<PageResult<EntityRecord>>>
{
    public async ValueTask<ErrorOr<PageResult<EntityRecord>>> Handle(
        ListRowsQuery query,
        CancellationToken cancellationToken = default)
    {
        var table = await tableRepository.SingleOrDefaultAsync(
            new TableByLogicalNameSpecification(query.Query.TableLogicalName),
            cancellationToken);

        if (table is null)
        {
            return DomainErrors.UnknownTable(query.Query.TableLogicalName);
        }

        var errors = queryValidationService.Validate(table, query.Query);
        if (errors.Count > 0)
        {
            return errors.ToList();
        }

        return await recordQueryService.ListAsync(query.Query, cancellationToken);
    }
}
