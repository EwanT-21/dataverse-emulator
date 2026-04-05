using Dataverse.Emulator.Domain.Queries;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class ListLinkedRowsHandler(
    LinkedRecordQueryService linkedRecordQueryService)
    : IQueryHandler<ListLinkedRowsQuery, ErrorOr<PageResult<LinkedEntityRecord>>>
{
    public async ValueTask<ErrorOr<PageResult<LinkedEntityRecord>>> Handle(
        ListLinkedRowsQuery query,
        CancellationToken cancellationToken = default)
        => await linkedRecordQueryService.ListAsync(query.Query, cancellationToken);
}
