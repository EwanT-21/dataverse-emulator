using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class GetRowByIdHandler(
    IReadRepository<EntityRecord> recordRepository)
    : IQueryHandler<GetRowByIdQuery, ErrorOr<EntityRecord>>
{
    public async ValueTask<ErrorOr<EntityRecord>> Handle(
        GetRowByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var record = await recordRepository.SingleOrDefaultAsync(
            new RecordByIdSpecification(query.TableLogicalName, query.Id),
            cancellationToken);

        return record is not null
            ? record
            : DomainErrors.RowNotFound(query.TableLogicalName, query.Id);
    }
}
